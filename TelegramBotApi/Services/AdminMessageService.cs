using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sqllite;
using Sqllite.Logger;
using System;
using System.Linq;
using System.Threading.Tasks;
using Utils;
using Utils.DbLogger;
using Utils.Logger;

namespace TelegramBotApi.Services
{
    public class AdminMessageService : IAdminMessageService
    {
        protected readonly SqlliteDbContext _dbContext;
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        protected readonly ILogger<AdminMessageService> _toFileLogger;
        protected readonly IConfigService _configService;
        protected readonly INewBotLogger _newLogger;
        public AdminMessageService(
            SqlliteDbContext dbContext,
            IBotLogger logger,
            IPhoneHelper phoneHelper,
            ILogger<AdminMessageService> toFileLogger,
            IConfigService configService,
            INewBotLogger newLogger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _newLogger = newLogger ?? throw new ArgumentNullException(nameof(newLogger));
        }

        public async Task GetUsers(long chatId)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            if (user == null)
            {
                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    "Подписка не активна.");
                return;
            }
            var clearedPhoneNumber = _phoneHelper.Clear(user.PhoneNumber);
            if (_configService.Config.Admins.Contains(clearedPhoneNumber))
            {
                var result = string.Join("\n", _dbContext.Users.Select(x => $"{x.Username} {_phoneHelper.Format(x.PhoneNumber)}").ToArray());
                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $"Список активных пользователей:\n{result}");
                await _logger.LogIncoming(LogConst.AdminRequestUsers, _phoneHelper.Format(user.PhoneNumber));
                await _newLogger.LogByType(MessageTypes.System, LogConst.UserRequestSubscribe, _phoneHelper.Format(user.PhoneNumber));

                return;
            }
            await _logger.LogIncoming(LogConst.RequestUsersButPermissionDenied, _phoneHelper.Format(user.PhoneNumber));
            await _newLogger.LogByType(MessageTypes.System, LogConst.RequestUsersButPermissionDenied, _phoneHelper.Format(user.PhoneNumber));
        }

        public async Task StartSending(long chatId)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            var clearedPhoneNumber = user != null ? _phoneHelper.Clear(user.PhoneNumber) : null;
            if (clearedPhoneNumber != null && _configService.Config.Admins.Contains(clearedPhoneNumber))
            {
                var state = _dbContext.States.First();

                if (state.IsEnabled == 1)
                {
                    await Bot.Api.SendTextMessageAsync
                        (chatId,
                        $"Рассылка уже включена");
                    await _logger.LogIncoming(LogConst.EnableSendingButItYetEnabled, clearedPhoneNumber);
                    await _newLogger.LogByType(MessageTypes.System, LogConst.EnableSendingButItYetEnabled, clearedPhoneNumber);
                    return;
                }

                state.IsEnabled = 1;
                _dbContext.States.Update(state);
                _dbContext.SaveChanges();

                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $"Рассылка возобновлена");
                await _logger.LogIncoming(LogConst.SendingEnabled, clearedPhoneNumber);
                await _newLogger.LogByType(MessageTypes.System, LogConst.SendingEnabled, clearedPhoneNumber);
                return;
            }

            await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $":) ");

            await _logger.LogIncoming(LogConst.EnableSendingButPermissionDenied, clearedPhoneNumber);
            await _newLogger.LogByType(MessageTypes.System, LogConst.EnableSendingButPermissionDenied, clearedPhoneNumber);
        }

        public async Task StopSending(long chatId)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            var clearedPhoneNumber = user != null ? _phoneHelper.Clear(user.PhoneNumber) : null;
            if (clearedPhoneNumber != null && _configService.Config.Admins.Contains(clearedPhoneNumber))
            {
                var state = _dbContext.States.First();

                if (state.IsEnabled == -1)
                {
                    await Bot.Api.SendTextMessageAsync
                        (chatId,
                        $"Рассылка уже выключена");
                    await _logger.LogIncoming(LogConst.DisableSendingButItYetDisabled, clearedPhoneNumber);
                    await _newLogger.LogByType(MessageTypes.System, LogConst.DisableSendingButItYetDisabled, clearedPhoneNumber);
                    return;
                }

                state.IsEnabled = -1;
                _dbContext.States.Update(state);
                _dbContext.SaveChanges();

                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $"Рассылка остановлена");
                await _logger.LogIncoming(LogConst.SendingDisabled, clearedPhoneNumber);
                await _newLogger.LogByType(MessageTypes.System, LogConst.SendingDisabled, clearedPhoneNumber);
                return;
            }

            await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $":) ");

            await _logger.LogIncoming(LogConst.DisableSendingButPermissionDenied, clearedPhoneNumber);
            await _newLogger.LogByType(MessageTypes.System, LogConst.DisableSendingButPermissionDenied, clearedPhoneNumber);
        }
    }
}
