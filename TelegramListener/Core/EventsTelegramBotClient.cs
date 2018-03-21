using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sqllite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramListener.Services;
using Utils;
using Utils.Logger;

namespace TelegramListener.Core
{
    public class EventsTelegramBotClient : TelegramBotClient
    {
        private readonly Config _config;
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        private readonly ILogger<EventsTelegramBotClient> _toFileLogger;
        protected readonly IPhoneNumberVerifier _phoneNumberVerifier;
        public EventsTelegramBotClient(
            Config config, IBotLogger logger, IPhoneHelper phoneHelper, ILogger<EventsTelegramBotClient> toFileLogger, IPhoneNumberVerifier phoneNumberVerifier) : base(config.BotApiKey)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneNumberVerifier = phoneNumberVerifier;
            OnUpdate += EventsTelegramBotClient_OnUpdate;
        }

        public void Start() {
            StartReceiving();
        }

        public void Stop() {
            StopReceiving();
        }

        private void EventsTelegramBotClient_OnUpdate(object sender, UpdateEventArgs e)
        {
            try {
                var message = e.Update.Message;
                var chatId = e.Update.Message.Chat.Id;

                if (message.Text == "/start")
                {
                    StartMessage(chatId, message.From.Username);
                    return;
                }

                if (message.Type == Telegram.Bot.Types.Enums.MessageType.ContactMessage)
                {
                    ContactMessage(chatId, message.From.Username, message.Contact.PhoneNumber).Wait();
                    return;
                }

                if (message.Text == "/bye")
                {
                    Unsubscribe(chatId);
                    return;
                }

                if (message.Text == "/get_users")
                {
                    GetUsers(chatId);
                    return;
                }

                if (message.Text == "/stop_sending")
                {
                    StopSending(chatId);
                    return;
                }

                if (message.Text == "/start_sending")
                {
                    StartSending(chatId);
                    return;
                }

                AllOtherMessages(chatId, message);

                
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err, err.Message);
            }
            
        }

        private void AllOtherMessages(long chatId, Telegram.Bot.Types.Message message)
        {
            using (var db = new UserContext(_config.DbPath))
            {

                var unauthUser = db.UnauthorizedUsers.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                if (unauthUser != null) {
                    if (message.Text == unauthUser.Code)
                    {
                        db.UnauthorizedUsers.Remove(unauthUser);

                        db.Users.Add(new User
                        {
                            ChatId = unauthUser.ChatId,
                            Username = unauthUser.Username,
                            PhoneNumber = unauthUser.PhoneNumber
                        });
                        var count = db.SaveChanges();

                        SendTextMessageAsync
                                    (chatId,
                                    "Авторизация пройдена", replyMarkup: ReplyMarkupRemoveButton);
                        _logger.LogIncoming($"Пользователь успешно авторизовался", _phoneHelper.Format(unauthUser.PhoneNumber));
                        return;
                    }
                    else if (message.Text == "Отправить код ещё раз") {
                        var code = _phoneNumberVerifier.SendVerifyRequest(unauthUser.PhoneNumber).Result;
                        unauthUser.Code = code.ToString();
                        var count = db.SaveChanges();
                        SendTextMessageAsync
                                    (chatId,
                                    "Код отправлен повторно", replyMarkup: ReplyMarkupResetCode);
                        return;
                    }
                    else {
                        SendTextMessageAsync
                                    (chatId,
                                    "Некорректный код", replyMarkup: ReplyMarkupResetCode);
                        _logger.LogIncoming($"Пользователь ввел некорректный код", _phoneHelper.Format(unauthUser.PhoneNumber));
                        return;
                    }
                }
                var user = db.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                string userStr;
                var auth = "";
                if (user == null)
                {
                    userStr = $"Username : {message.From.Username} , First Name = {message.From.FirstName}, Last NAme = {message.From.LastName} ";
                }
                else
                {
                    userStr = $"{_phoneHelper.Format(user.PhoneNumber)}";
                }

                if (message.Type != Telegram.Bot.Types.Enums.MessageType.TextMessage && message.Type != Telegram.Bot.Types.Enums.MessageType.ContactMessage)
                {
                    _logger.LogIncoming($"Пришло сообщение неподдерживаемого типа", _phoneHelper.Format(user.PhoneNumber));
                    SendTextMessageAsync
                                (chatId,
                                _config.UnsupportedMessageType, replyMarkup: ReplyMarkupRemoveButton);
                    return;
                }

                _logger.LogIncoming($"Пришло сообщение: { message.Text }", userStr);

                SendTextMessageAsync
                (chatId,
                $"{_config.AutoresponseText}", replyMarkup: ReplyMarkupRemoveButton);
            }
        }

        private void StartSending(long chatId)
        {
            using (var db = new UserContext(_config.DbPath))
            {

                var user = db.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(user.PhoneNumber);
                if (_config.Admins.Contains(clearedPhoneNumber))
                {
                    using (var states = new StateContext(_config.DbPath))
                    {
                        if (states.States.First().IsEnabled == 1)
                        {
                            _logger.LogIncoming($"Попытка включить рассылку, которая уже работает", _phoneHelper.Format(user.PhoneNumber));

                            SendTextMessageAsync
                                (chatId,
                                $"Рассылка уже включена", replyMarkup: ReplyMarkupRemoveButton);
                            return;
                        }
                        var state = states.States.First();
                        state.IsEnabled = 1;
                        states.States.Update(state);
                        states.SaveChanges();

                        _logger.LogIncoming($"Рассылка возобновлена", _phoneHelper.Format(user.PhoneNumber));
                        SendTextMessageAsync
                            (chatId,
                            $"Рассылка возобновлена", replyMarkup: ReplyMarkupRemoveButton);
                    }
                }
            }
        }

        private void StopSending(long chatId)
        {
            using (var db = new UserContext(_config.DbPath))
            {
                var user = db.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(user.PhoneNumber);
                if (_config.Admins.Contains(clearedPhoneNumber))
                {
                    using (var states = new StateContext(_config.DbPath))
                    {
                        if (states.States.First().IsEnabled == -1)
                        {
                            _logger.LogIncoming($"Попытка остановить рассылку, которая уже остановлена", _phoneHelper.Format(user.PhoneNumber));

                            SendTextMessageAsync
                                (chatId,
                                $"Рассылка уже была остановлена и ещё не запущена",
                                replyMarkup: ReplyMarkupRemoveButton);

                            return;
                        }

                        var state = states.States.First();
                        state.IsEnabled = -1;
                        states.States.Update(state);
                        states.SaveChanges();

                        _logger.LogIncoming($"Рассылка остановлена", _phoneHelper.Format(user.PhoneNumber));
                        SendTextMessageAsync
                            (chatId,
                            $"Рассылка остановлена",
                            replyMarkup: ReplyMarkupRemoveButton);
                    }
                }
            }
        }

        private void GetUsers(long chatId)
        {
            using (var db = new UserContext(_config.DbPath))
            {

                var user = db.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                if (user == null)
                {
                    SendTextMessageAsync
                    (chatId,
                    "Подписка не активна.",
                    replyMarkup: ReplyMarkupRemoveButton);
                    return;
                }
                var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(user.PhoneNumber);
                if (_config.Admins.Contains(clearedPhoneNumber))
                {
                    _logger.LogIncoming($"Запрос списка пользователей от администратора", _phoneHelper.Format(user.PhoneNumber));

                    var result = string.Join("\n", db.Users.Select(x => $"{x.Username} {_phoneHelper.Format(x.PhoneNumber)}").ToArray());
                    SendTextMessageAsync
                        (chatId,
                        $"Список активных пользователей:\n{result}",
                        replyMarkup: ReplyMarkupRemoveButton);
                }
            }
        }

        private void StartMessage(long chatId, string username) {

                using (var db = new UserContext(_config.DbPath))
                {
                    var user = db.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                    if (user != null)
                    {

                        SendTextMessageAsync(chatId,
                            _config.AlreadySubscribedMessage);
                        return;
                    }
                }

                _logger.LogAuth("Пользователь запрашивает подписку", username);
                SendTextMessageAsync
                    (chatId,
                    _config.HelloMessage,
                    replyMarkup: keyboard);
        }

        private async Task ContactMessage(long chatId, string username, string phoneNumber) {
            var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(phoneNumber);
            using (var db = new UserContext(_config.DbPath)) {
                if (db.Users.Any(x => x.PhoneNumber == clearedPhoneNumber))
                {
                    await SendTextMessageAsync(chatId,
                        "Контакт уже есть в списке.");
                    return;
                }

                if (db.UnauthorizedUsers.Any(x => x.PhoneNumber == clearedPhoneNumber)) {
                    await SendTextMessageAsync(chatId,
                    "На ваш телефон отправлен код подтверждения, отправьте его сюда");
                    return;
                }

                var code = await _phoneNumberVerifier.SendVerifyRequest(clearedPhoneNumber);
                db.UnauthorizedUsers.Add(new UnauthorizedUser
                {
                    ChatId = chatId.ToString(),
                    Username = username,
                    PhoneNumber = clearedPhoneNumber,
                    Code = code.ToString()
                });

                var count = db.SaveChanges();

                await SendTextMessageAsync(chatId,
                    "На ваш телефон отправлен код подтверждения, отправьте его сюда",
                    replyMarkup: ReplyMarkupResetCode);
            }


            //using (var db = new UserContext(_config.DbPath))
            //{

            //    if (db.Users.Any(x => x.PhoneNumber == clearedPhoneNumber))
            //    {
            //        SendTextMessageAsync(chatId,
            //            "Контакт сохранен.");
            //        return;
            //    }


            //    db.Users.Add(new User
            //    {
            //        ChatId = chatId.ToString(),
            //        Username = username,
            //        PhoneNumber = clearedPhoneNumber
            //    });
            //    var count = db.SaveChanges();

            //    _logger.LogAuth("Пользователь подписался", _phoneHelper.Format(phoneNumber));

            //    SendTextMessageAsync
            //    (chatId,
            //    _config.UserSubscribed);
            //}
        }

        private void Unsubscribe(long chatId) {
            using (var db = new UserContext(_config.DbPath))
            {
                var user = db.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
                if (user == null)
                {
                    SendTextMessageAsync
                    (chatId,
                    "Подписка не активна.",
                    replyMarkup: ReplyMarkupRemoveButton);
                    return;
                }


                db.Users.RemoveRange(user);
                var count = db.SaveChanges();

                _logger.LogAuth("Пользователь отписался", _phoneHelper.Format(user.PhoneNumber));

                SendTextMessageAsync
                (chatId,
                _config.UserUnsubscribed,
                replyMarkup: ReplyMarkupRemoveButton);
            }
        }

        private ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup
        {
            Keyboard = new[] {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Поделиться номером телефона") {
                                RequestContact = true,
                            },
                        },
                    },
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        private ReplyKeyboardMarkup ReplyMarkupResetCode = new ReplyKeyboardMarkup
        {
            Keyboard = new[] {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Отправить код ещё раз"),
                        },
                    },
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
        private ReplyKeyboardRemove ReplyMarkupRemoveButton = new ReplyKeyboardRemove() { RemoveKeyboard = true };
    }
}
