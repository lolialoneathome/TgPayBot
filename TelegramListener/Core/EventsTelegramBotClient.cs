using PayBot.Configuration;
using Sqllite;
using System;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;
using Utils.Logger;

namespace TelegramListener.Core
{
    public class EventsTelegramBotClient : TelegramBotClient
    {
        private readonly Config _config;
        protected readonly IBotLogger _logger;
        public EventsTelegramBotClient(Config config, IBotLogger logger) : base(config.BotApiKey)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            var message = e.Update.Message;
            if (message.Text == "/start")
            {
                var keyboard = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup
                {
                    Keyboard = new[] {
                        new[]
                        {
                            new Telegram.Bot.Types.KeyboardButton("Поделиться номером телефона") {
                                RequestContact = true
                            },
                        },
                    },
                    ResizeKeyboard = true
                };

                SendTextMessageAsync
                    (e.Update.Message.Chat.Id, 
                    _config.HelloMessage,
                    replyMarkup: keyboard);

                return;
            }
            if (message.Type == Telegram.Bot.Types.Enums.MessageType.ContactMessage)
            {
                var ReplyMarkupRemoveButton = new ReplyKeyboardRemove() { RemoveKeyboard = true };

                using (var db = new UserContext(_config.DbPath))
                {
                    if (db.Users.Any(x => x.PhoneNumber == e.Update.Message.Contact.PhoneNumber))
                        return;

                    db.Users.Add(new User
                    {
                        ChatId = e.Update.Message.Chat.Id.ToString(),
                        Username = e.Update.Message.From.Username,
                        PhoneNumber = e.Update.Message.Contact.PhoneNumber
                    });
                    var count = db.SaveChanges();

                    _logger.LogAuth("Пользователь подписался", e.Update.Message.Contact.PhoneNumber);

                    SendTextMessageAsync
                    (e.Update.Message.Chat.Id, 
                    "Отлично, подписка активна! Я обязательно тебе напишу, если у меня будут для тебя новости! Если ты хочешь отписаться от рассылки - набери /bye в чат", 
                    replyMarkup: ReplyMarkupRemoveButton);
                }

                return;
            }

            if (message.Text == "/bye")
            {
                using (var db = new UserContext(_config.DbPath))
                {
                    var user = db.Users.Where(x => x.ChatId == e.Update.Message.Chat.Id.ToString()).SingleOrDefault();
                    if (user == null)
                        return;

                    db.Users.RemoveRange(user);
                    var count = db.SaveChanges();

                    _logger.LogAuth("Пользователь отписался", user.PhoneNumber);

                    SendTextMessageAsync
                    (e.Update.Message.Chat.Id,
                    "Ок, подписка отключена. Возвращайся! Для возвращения просто набери /start");
                }

                return;
            }

            if (message.Text == "/get_users")
            {
                using (var db = new UserContext(_config.DbPath))
                {

                    var user = db.Users.Where(x => x.ChatId == e.Update.Message.Chat.Id.ToString()).SingleOrDefault();
                    if (_config.Admins.Contains(user.PhoneNumber)) {
                        _logger.LogIncoming($"Запрос списка пользователей от администратора", user.PhoneNumber);

                        var result = string.Join("\n",  db.Users.Select(x => $"{x.Username} {x.PhoneNumber}").ToArray());
                        SendTextMessageAsync
                            (e.Update.Message.Chat.Id,
                            $"Список активных пользователей:\n{result}");

                        return;
                    }
                }
            }

            if (message.Text == "/stop_sending")
            {
                using (var db = new UserContext(_config.DbPath))
                {

                    var user = db.Users.Where(x => x.ChatId == e.Update.Message.Chat.Id.ToString()).SingleOrDefault();
                    if (_config.Admins.Contains(user.PhoneNumber))
                    {
                        using (var states = new StateContext(_config.DbPath))
                        {
                            if (states.States.First().IsEnabled == -1) {
                                _logger.LogIncoming($"Попытка остановить рассылку, которая уже остановлена", user.PhoneNumber);

                                SendTextMessageAsync
                                    (e.Update.Message.Chat.Id,
                                    $"Рассылка уже была остановлена и ещё не запущена");

                                return;
                            }

                            var state = states.States.First();
                            state.IsEnabled = -1;
                            states.States.Update(state);
                            states.SaveChanges();

                            _logger.LogIncoming($"Рассылка остановлена", user.PhoneNumber);
                            SendTextMessageAsync
                                (e.Update.Message.Chat.Id,
                                $"Рассылка остановлена");
                            return;
                        }
                    }
                }
            }

            if (message.Text == "/start_sending")
            {
                using (var db = new UserContext(_config.DbPath))
                {

                    var user = db.Users.Where(x => x.ChatId == e.Update.Message.Chat.Id.ToString()).SingleOrDefault();
                    if (_config.Admins.Contains(user.PhoneNumber))
                    {
                        using (var states = new StateContext(_config.DbPath))
                        {
                            if (states.States.First().IsEnabled == 1)
                            {
                                _logger.LogIncoming($"Попытка включить рассылку, которая уже работает", user.PhoneNumber);

                                SendTextMessageAsync
                                    (e.Update.Message.Chat.Id,
                                    $"Рассылка уже включена");
                                return;
                            }
                            var state = states.States.First();
                            state.IsEnabled = 1;
                            states.States.Update(state);
                            states.SaveChanges();

                            _logger.LogIncoming($"Рассылка возобновлена", user.PhoneNumber);
                            SendTextMessageAsync
                                (e.Update.Message.Chat.Id,
                                $"Рассылка возобновлена");
                            return;
                        }
                    }
                }
            }

            using (var db = new UserContext(_config.DbPath))
            {
                
                var user = db.Users.Where(x => x.ChatId == e.Update.Message.Chat.Id.ToString()).SingleOrDefault();
                string userStr;
                var auth = "";
                if (user == null)
                {
                    auth = "Для подписки отправь команду /start";
                    userStr = $"Username : {e.Update.Message.From.Username} , First Name = {e.Update.Message.From.FirstName}, Last NAme = {e.Update.Message.From.LastName} ";
                }
                else
                {
                    auth = "Для отписки отправь команду /bye";
                    userStr = $"{user.PhoneNumber}";
                }
                
                _logger.LogIncoming($"Пришло сообщение {e.Update.Message.Text }", userStr);

                SendTextMessageAsync
                (e.Update.Message.Chat.Id,
                $"{_config.AutoresponseText}\n{auth}");
            }
        }
    }
}
