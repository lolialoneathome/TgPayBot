using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sqllite;
using Sqllite.Logger;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using Utils;
using Utils.DbLogger;
using Utils.Logger;

namespace TelegramBotApi.Services
{
    public class UserMessageService : IUserMessageService
    {
        protected readonly SqlliteDbContext _dbContext;
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        protected readonly ILogger<UserMessageService> _toFileLogger;
        protected readonly IConfigService _configService;
        protected readonly IPhoneNumberVerifier _phoneNumberVerifier;
        protected readonly INewBotLogger _newLogger;
        public UserMessageService(
            SqlliteDbContext dbContext,
            IBotLogger logger,
            IPhoneHelper phoneHelper,
            ILogger<UserMessageService> toFileLogger,
            IConfigService configService,
            IPhoneNumberVerifier phoneNumberVerifier,
            INewBotLogger newBotLogger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneNumberVerifier = phoneNumberVerifier ?? throw new ArgumentNullException(nameof(phoneNumberVerifier));
            _newLogger = newBotLogger ?? throw new ArgumentNullException(nameof(newBotLogger));
        }

        public async Task RequestSubscribe(long chatId, string username)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user != null)
            {
                await Bot.Api.SendTextMessageAsync(chatId,
                    _configService.Config.AlreadySubscribedMessage);
                await _logger.LogAuth(LogConst.UserAlreadySubscribetButSendStart, _phoneHelper.Format(user.PhoneNumber));
                await _newLogger.LogByType(MessageTypes.Incoming, LogConst.UserRequestSubscribe, user.PhoneNumber);
                return;
            }

            await Bot.Api.SendTextMessageAsync
                    (chatId,
                    _configService.Config.HelloMessage,
                    replyMarkup: phoneNumberKeyboard);
            await _logger.LogAuth(LogConst.UserRequestSubscribe, username);
            await _newLogger.LogByType(MessageTypes.Auth, LogConst.UserRequestSubscribe, username);
        }

        public async Task ReceivedContact(long chatId, string username, string phone)
        {
            var clearedPhoneNumber = _phoneHelper.Clear(phone);
            if (_dbContext.Users.Any(x => x.ChatId == chatId.ToString()))
            {
                await Bot.Api.SendTextMessageAsync(chatId,
                    "Вы уже подписаны на рассылку");
                await _logger.LogAuth(LogConst.UserAlreadySubscribetButSendContact, _phoneHelper.Format(clearedPhoneNumber));
                await _newLogger.LogByType(MessageTypes.Incoming, LogConst.UserAlreadySubscribetButSendContact, clearedPhoneNumber);
                return;
            }

            if (_dbContext.UnauthorizedUsers.Any(x => x.ChatId == chatId.ToString()))
            { 
                await Bot.Api.SendTextMessageAsync(chatId,
                    "На ваш телефон отправлен код подтверждения, отправьте его сюда",
                    replyMarkup: ReplyMarkupResetCode);
                await _logger.LogAuth(LogConst.UserNotSubscribedAndGetCodeButSendContact,
                    username);
                await _newLogger.LogByType(MessageTypes.Auth, LogConst.UserNotSubscribedAndGetCodeButSendContact, username);
                return;
            }

            var code = await _phoneNumberVerifier.SendVerifyRequest(clearedPhoneNumber);
            _dbContext.UnauthorizedUsers.Add(new UnauthorizedUser
            {
                ChatId = chatId.ToString(),
                Username = username,
                PhoneNumber = clearedPhoneNumber,
                Code = code.ToString()
            });
            _dbContext.SaveChanges();

            await Bot.Api.SendTextMessageAsync(chatId,
                "На ваш телефон отправлен код подтверждения, отправьте его сюда",
                replyMarkup: ReplyMarkupResetCode);
            await _logger.LogAuth(LogConst.CodeSendedToUser, _phoneHelper.Format(clearedPhoneNumber));
            await _newLogger.LogByType(MessageTypes.Auth, LogConst.CodeSendedToUser, clearedPhoneNumber);
        }

        public async Task Unsubscribe(long chatId, string username)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user == null)
            {
                await Bot.Api.SendTextMessageAsync
                    (chatId, "Подписка не активна.");
                await _logger.LogAuth(LogConst.UnsubscribedUserSendBye, username);
                await _newLogger.LogByType(MessageTypes.Incoming, LogConst.UnsubscribedUserSendBye, username);
                return;
            }
            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();
            await Bot.Api.SendTextMessageAsync
                (chatId,
                _configService.Config.UserUnsubscribed);

            await _logger.LogAuth(LogConst.Unsubscribe, _phoneHelper.Format(user.PhoneNumber));
            await _newLogger.LogByType(MessageTypes.Auth, LogConst.Unsubscribe, user.PhoneNumber);
        }

        public async Task ReceiveTextMessage(long chatId, string text, string username)
        {
            var unauthUser = _dbContext.UnauthorizedUsers.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (unauthUser != null)
            {
                await ReceiveVerifyPhoneNumberCode(unauthUser, text);
                return;
            }

            string from = username;
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user != null)
                from = user.PhoneNumber;
            await Bot.Api.SendTextMessageAsync
                (chatId,
                $"{_configService.Config.AutoresponseText}");

            await _logger.LogIncoming($"{LogConst.MessageReceived}: { text }", from);
            await _newLogger.LogByType(MessageTypes.Incoming, $"{LogConst.MessageReceived}: { text }", from);
        }

        public async Task ReceiveVerifyPhoneNumberCode(UnauthorizedUser unauthUser, string code)
        {
            if (unauthUser.Code == code)
            {
                _dbContext.UnauthorizedUsers.Remove(unauthUser);

                _dbContext.Users.Add(new User
                {
                    ChatId = unauthUser.ChatId,
                    Username = unauthUser.Username,
                    PhoneNumber = unauthUser.PhoneNumber
                });
                await _dbContext.SaveChangesAsync();

                await Bot.Api.SendTextMessageAsync
                    (unauthUser.ChatId, 
                    _configService.Config.UserSubscribed, 
                    replyMarkup: ReplyMarkupRemoveKeyboard);
                await _logger.LogAuth(LogConst.CorrectCodeSuccessAuth, _phoneHelper.Format(unauthUser.PhoneNumber));
                await _newLogger.LogByType(MessageTypes.Auth, LogConst.CorrectCodeSuccessAuth, unauthUser.PhoneNumber);
            }
            else
            {
                await Bot.Api.SendTextMessageAsync
                                    (unauthUser.ChatId,
                                    "Некорректный код");
                await _logger.LogAuth($"{LogConst.IncorrectCode}. Отправлено на телефон: {unauthUser.Code}, пользователь ввел {code}",
                    _phoneHelper.Format(unauthUser.PhoneNumber));
                await _newLogger.LogByType(MessageTypes.Auth, $"{LogConst.IncorrectCode}. Отправлено на телефон: {unauthUser.Code}, пользователь ввел {code}",
                    unauthUser.PhoneNumber);
            }
        }

        public async Task ReceiveUnsupportedMessage(long chatId, string from)
        {
            await Bot.Api.SendTextMessageAsync
                        (chatId,
                        _configService.Config.UnsupportedMessageType);
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user != null)
                from = user.PhoneNumber;
            await _logger.LogIncoming(LogConst.UnsupportedTypeMessage, from);
            await _newLogger.LogByType(MessageTypes.Incoming, LogConst.UnsupportedTypeMessage, from);
        }

        public async Task ResetCode(long chatId, string from)
        {
            var unauthUser = _dbContext.UnauthorizedUsers.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (unauthUser != null)
            {
                var code = await _phoneNumberVerifier.SendVerifyRequest(unauthUser.PhoneNumber);
                unauthUser.Code = code.ToString();
                await _dbContext.SaveChangesAsync();
                await Bot.Api.SendTextMessageAsync
                            (chatId, "Код отправлен повторно", replyMarkup: ReplyMarkupResetCode);
                await _logger.LogAuth(LogConst.RefreshCode, _phoneHelper.Format(unauthUser.PhoneNumber));
                await _newLogger.LogByType(MessageTypes.Auth, LogConst.RefreshCode, unauthUser.PhoneNumber);
                return;
            }

            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user != null)
            {
                await Bot.Api.SendTextMessageAsync
                            (chatId, _configService.Config.AlreadySubscribedMessage, replyMarkup: ReplyMarkupResetCode);
                await _logger.LogAuth(LogConst.RefreshCodeButYetSubscribed, _phoneHelper.Format(user.PhoneNumber));
                await _newLogger.LogByType(MessageTypes.Incoming, LogConst.RefreshCodeButYetSubscribed, unauthUser.PhoneNumber);
                return;
            }

            await Bot.Api.SendTextMessageAsync
                            (chatId, _configService.Config.HelloMessage, replyMarkup: phoneNumberKeyboard);

            await _logger.LogAuth(LogConst.RefreshCodeButUnsubscribed, from);
            await _newLogger.LogByType(MessageTypes.Incoming, LogConst.RefreshCodeButUnsubscribed, from);
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
