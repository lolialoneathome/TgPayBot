using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sqllite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Utils;
using Utils.Logger;

namespace TelegramBotApi.Services
{
    public class UserMessageService : IUserMessageService
    {
        protected readonly UserContext _userContext;
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        protected readonly ILogger<UserMessageService> _toFileLogger;
        protected readonly IConfigService _configService;
        protected readonly IPhoneNumberVerifier _phoneNumberVerifier;
        public UserMessageService(
            UserContext userContext,
            IBotLogger logger,
            IPhoneHelper phoneHelper,
            ILogger<UserMessageService> toFileLogger,
            IConfigService configService,
            IPhoneNumberVerifier phoneNumberVerifier)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneNumberVerifier = phoneNumberVerifier ?? throw new ArgumentNullException(nameof(phoneNumberVerifier));
        }

        public async Task RequestSubscribe(long chatId, string username)
        {
            var user = _userContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user != null)
            {
                await Bot.Api.SendTextMessageAsync(chatId,
                    _configService.Config.AlreadySubscribedMessage);
                await _logger.LogAuth("Пользователь, который уже подписан, повторно отправил сообщение /start", _phoneHelper.Format(user.PhoneNumber));
                return;
            }

            await _logger.LogAuth("Пользователь запрашивает подписку", username);
            await Bot.Api.SendTextMessageAsync
                    (chatId,
                    _configService.Config.HelloMessage,
                    replyMarkup: phoneNumberKeyboard);
        }

        public async Task ReceivedContact(long chatId, string username, string phone)
        {
            var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(phone);
            if (_userContext.Users.Any(x => x.PhoneNumber == clearedPhoneNumber))
            {
                await Bot.Api.SendTextMessageAsync(chatId,
                    "Контакт уже есть в списке.");
                await _logger.LogAuth("Пользователь, который уже авторизован, повторно отправил номер телефона", _phoneHelper.Format(clearedPhoneNumber));
                return;
            }

            if (_userContext.UnauthorizedUsers.Any(x => x.PhoneNumber == clearedPhoneNumber))
            {
                await Bot.Api.SendTextMessageAsync(chatId,
                    "На ваш телефон отправлен код подтверждения, отправьте его сюда",
                    replyMarkup: ReplyMarkupResetCode);
                await _logger.LogAuth("Пользователь, который еше не авторизован, но которому уже был отправлен код, повторно отправил номер телефона",
                    _phoneHelper.Format(clearedPhoneNumber));
                return;
            }

            var code = await _phoneNumberVerifier.SendVerifyRequest(clearedPhoneNumber);
            _userContext.UnauthorizedUsers.Add(new UnauthorizedUser
            {
                ChatId = chatId.ToString(),
                Username = username,
                PhoneNumber = clearedPhoneNumber,
                Code = code.ToString()
            });
            _userContext.SaveChanges();

            await Bot.Api.SendTextMessageAsync(chatId,
                "На ваш телефон отправлен код подтверждения, отправьте его сюда",
                replyMarkup: ReplyMarkupResetCode);
            await _logger.LogAuth("Пользователю отправлен код", _phoneHelper.Format(clearedPhoneNumber));
        }

        public async Task Unsubscribe(long chatId, string username)
        {
            var user = _userContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user == null)
            {
                await Bot.Api.SendTextMessageAsync
                    (chatId, "Подписка не активна.");
                await _logger.LogAuth("Пользователь, который не подписан, отправил сообщение /bye", username);
                return;
            }
            _userContext.Users.Remove(user);
            await _userContext.SaveChangesAsync();

            await _logger.LogAuth("Пользователь отписался", _phoneHelper.Format(user.PhoneNumber));

            await Bot.Api.SendTextMessageAsync
                (chatId,
                _configService.Config.UserUnsubscribed);
        }

        public async Task ReceiveTextMessage(long chatId, string text, string username)
        {
            var unauthUser = _userContext.UnauthorizedUsers.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (unauthUser != null)
            {
                await ReceiveVerifyPhoneNumberCode(unauthUser, text);
                return;
            }

            await _logger.LogIncoming($"Пришло сообщение: { text }", username);

            await Bot.Api.SendTextMessageAsync
                (chatId,
                $"{_configService.Config.AutoresponseText}");
        }

        public async Task ReceiveVerifyPhoneNumberCode(UnauthorizedUser unauthUser, string code)
        {
            if (unauthUser.Code == code)
            {
                _userContext.UnauthorizedUsers.Remove(unauthUser);

                _userContext.Users.Add(new User
                {
                    ChatId = unauthUser.ChatId,
                    Username = unauthUser.Username,
                    PhoneNumber = unauthUser.PhoneNumber
                });
                await _userContext.SaveChangesAsync();

                await Bot.Api.SendTextMessageAsync
                    (unauthUser.ChatId, 
                    _configService.Config.UserSubscribed, 
                    replyMarkup: ReplyMarkupRemoveKeyboard);
                await _logger.LogAuth($"Пользователь ввёл корректный код и успешно авторизовался", _phoneHelper.Format(unauthUser.PhoneNumber));
            }
            else
            {
                await Bot.Api.SendTextMessageAsync
                                    (unauthUser.ChatId,
                                    "Некорректный код");
                await _logger.LogAuth($"Пользователь ввел некорректный код. Отправлено на телефон: {unauthUser.Code}, пользователь ввел {code}",
                    _phoneHelper.Format(unauthUser.PhoneNumber));
            }
        }

        public async Task ReceiveUnsupportedMessage(long chatId, string from)
        {
            await _logger.LogIncoming($"Пришло сообщение неподдерживаемого типа", from);
            await Bot.Api.SendTextMessageAsync
                        (chatId,
                        _configService.Config.UnsupportedMessageType);
        }

        public async Task ResetCode(long chatId)
        {
            var unauthUser = _userContext.UnauthorizedUsers.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (unauthUser != null)
            {
                var code = await _phoneNumberVerifier.SendVerifyRequest(unauthUser.PhoneNumber);
                unauthUser.Code = code.ToString();
                await _userContext.SaveChangesAsync();
                await Bot.Api.SendTextMessageAsync
                            (chatId, "Код отправлен повторно", replyMarkup: ReplyMarkupResetCode);
                await _logger.LogAuth($"Пользователь запросил код повторно", _phoneHelper.Format(unauthUser.PhoneNumber));
                return;
            }

            var user = _userContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user != null)
            {
                await Bot.Api.SendTextMessageAsync
                            (chatId, _configService.Config.AlreadySubscribedMessage, replyMarkup: ReplyMarkupResetCode);
                await _logger.LogAuth($"Пользователь запросил код повторно, но он уже авторизован в системе", _phoneHelper.Format(user.PhoneNumber));
                return;
            }

            await Bot.Api.SendTextMessageAsync
                            (chatId, _configService.Config.HelloMessage, replyMarkup: phoneNumberKeyboard);
            await _logger.LogAuth($"Неизвестный пользователь отправил запрос на сброс кода", _phoneHelper.Format(unauthUser.PhoneNumber));
        }

        private ReplyKeyboardMarkup phoneNumberKeyboard = new ReplyKeyboardMarkup
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

        private ReplyKeyboardRemove ReplyMarkupRemoveKeyboard = new ReplyKeyboardRemove();

    }
}
