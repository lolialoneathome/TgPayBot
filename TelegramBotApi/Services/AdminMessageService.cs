using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sqllite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Utils;
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
        public AdminMessageService(
            SqlliteDbContext dbContext,
            IBotLogger logger,
            IPhoneHelper phoneHelper,
            ILogger<AdminMessageService> toFileLogger,
            IConfigService configService)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
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
            var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(user.PhoneNumber);
            if (_configService.Config.Admins.Contains(clearedPhoneNumber))
            {
                await _logger.LogIncoming($"Запрос списка пользователей от администратора", _phoneHelper.Format(user.PhoneNumber));

                var result = string.Join("\n", _dbContext.Users.Select(x => $"{x.Username} {_phoneHelper.Format(x.PhoneNumber)}").ToArray());
                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $"Список активных пользователей:\n{result}");
                return;
            }
            await _logger.LogIncoming($"Попытка запросить пользователей из под учетки, у которой нет админских прав", _phoneHelper.Format(user.PhoneNumber));
        }

        public async Task StartSending(long chatId)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(user.PhoneNumber);
            if (_configService.Config.Admins.Contains(clearedPhoneNumber))
            {
                var state = _dbContext.States.First();

                if (state.IsEnabled == 1)
                {
                    await _logger.LogIncoming($"Попытка включить рассылку, которая уже работает", _phoneHelper.Format(user.PhoneNumber));

                    await Bot.Api.SendTextMessageAsync
                        (chatId,
                        $"Рассылка уже включена");
                    return;
                }

                state.IsEnabled = 1;
                _dbContext.States.Update(state);
                _dbContext.SaveChanges();

                await _logger.LogIncoming($"Рассылка возобновлена", _phoneHelper.Format(user.PhoneNumber));
                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $"Рассылка возобновлена");
                return;
            }

            await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $":) ");

            await _logger.LogIncoming($"Попытка включить рассылку из под учетки, у которой нет прав", _phoneHelper.Format(user.PhoneNumber));
        }

        public async Task StopSending(long chatId)
        {
            var user = _dbContext.Users.Where(x => x.ChatId == chatId.ToString()).SingleOrDefault();
            var clearedPhoneNumber = _phoneHelper.GetOnlyNumerics(user.PhoneNumber);
            if (_configService.Config.Admins.Contains(clearedPhoneNumber))
            {
                var state = _dbContext.States.First();

                if (state.IsEnabled == -1)
                {
                    await _logger.LogIncoming($"Попытка выключить рассылку, которая уже остановлена", _phoneHelper.Format(user.PhoneNumber));

                    await Bot.Api.SendTextMessageAsync
                        (chatId,
                        $"Рассылка уже выключена");
                    return;
                }

                state.IsEnabled = -1;
                _dbContext.States.Update(state);
                _dbContext.SaveChanges();

                await _logger.LogIncoming($"Рассылка остановлена", _phoneHelper.Format(user.PhoneNumber));
                await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $"Рассылка остановлена");
                return;
            }

            await Bot.Api.SendTextMessageAsync
                    (chatId,
                    $":) ");

            await _logger.LogIncoming($"Попытка выключить рассылку из под учетки, у которой нет прав", _phoneHelper.Format(user.PhoneNumber));
        }
    }
}
