using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sender.DataSource.Base;
using Sender.DataSource.GoogleTabledataSource;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Utils;
using Utils.Logger;

namespace Sender.Services
{
    public class SenderService : ISenderService
    {
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        private readonly ILogger<SenderService> _toFileLogger;
        protected readonly IMessageDataSource _messageDataSource;

        protected readonly IConfigService _configService;
        protected readonly UserContext _userContext;
        protected readonly StateContext _stateContext;
        public SenderService
            (IConfigService configService,
            IBotLogger logger,
            IPhoneHelper phoneHelper,
            ILogger<SenderService> toFileLogger,
            IMessageDataSource messageDataSource,
            UserContext userContext,
            StateContext stateContext)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _stateContext = stateContext ?? throw new ArgumentNullException(nameof(stateContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _messageDataSource = messageDataSource ?? throw new ArgumentNullException(nameof(messageDataSource));
        }

        protected Config _config => _configService.Config;

        private bool IsListValid()
        {
            foreach (var spreedSheet in _config.Spreadsheets) {
                foreach (var list in spreedSheet.Lists)
                {
                    if (!Regex.IsMatch(list.Date, @"^[a-zA-Z]+$"))
                        return false;
                    if (!Regex.IsMatch(list.IsSendedColumn, @"^[a-zA-Z]+$"))
                        return false;
                    if (!Regex.IsMatch(list.MessageText, @"^[a-zA-Z]+$"))
                        return false;
                    if (!Regex.IsMatch(list.Status, @"^[a-zA-Z]+$"))
                        return false;
                    if (!Regex.IsMatch(list.TgUser, @"^[a-zA-Z]+$"))
                        return false;
                }
            }
            return true;
        }

        public async Task Process(CancellationToken cancellation)
        {
            var errorList = new List<string>();
            var sendedList = new List<SendedMessage>();
            var isErrorSended = false;
            var isSuccessSended = false;
            try
            {
                if (!CheckEnable())
                {
                    _toFileLogger.LogInformation("Sendidng stoped. Do nothing.");
                    _logger.LogSended($"Рассылка остановлена, ничего не отправляю", null);
                    return;
                }
                _toFileLogger.LogInformation("Start sending...");
                _logger.LogSystem($"Начинаю отправку сообщений...", null);

                if (!IsListValid())
                    throw new InvalidOperationException("Invalid config file!");

                var rows = await _messageDataSource.GetMessages(_config);
                var sendedMesageCount = 0;
                if (rows != null)
                {
                    var rowsForUpdate = new Dictionary<string, List<IMessage>>();
                    foreach (var row in rows.OrderBy(x => x.LastModifiedDate))
                    {
                        {
                            var text = row.Text;
                            if (row.To == null)
                            {
                                var list = Regex.Match(row.CellForUpdate, @"\d+").Value;
                                var rownum = Regex.Match(row.CellForUpdate, @"^(.*?)!").Value;
                                errorList.Add
                                    ($"У пользователя в таблице {row.Table } на листе {list} в строке {rownum} не указан номер телефона, сообщение НЕ отправлено!");
                                continue;
                            }

                            if (_phoneHelper.IsPhone(row.To))
                            {
                                row.To = _phoneHelper.Format(row.To);
                            }
                            var phone = row.To;
                            var sendMessageResult = await SendMessageAsync(row.SenderType, text, phone, _config.DbPath, _config.BotApiKey);
                            if (sendMessageResult != null)
                            {
                                sendedList.Add(sendMessageResult);
                                if (!rowsForUpdate.ContainsKey(row.Table))
                                    rowsForUpdate[row.Table] = new List<IMessage>();
                                rowsForUpdate[row.Table].Add(row);
                                sendedMesageCount++;
                            }
                        }
                    }

                    if (rowsForUpdate.Count > 0)
                    {
                        foreach (var item in rowsForUpdate)
                        {
                            var updateResult = await _messageDataSource.UpdateMessageStatus(item.Value);
                        }
                    }
                    _logger.LogErrorList(errorList);
                    isErrorSended = true;
                    _logger.LogSendedList(sendedList);
                    isSuccessSended = true;

                }
                _logger.LogSystem($"Отправка сообщений закончена. Сообщений отправлено: {sendedMesageCount}", null);
                _toFileLogger.LogInformation($"End sending. Message count: {sendedMesageCount}");
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err, err.Message);
                if (!isErrorSended)
                    _logger.LogErrorList(errorList);
                if (!isSuccessSended)
                    _logger.LogSendedList(sendedList);
                _logger.LogError($"Произошла непредвиденная ошибка во время отправки сообщений! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}");
            }
        }

        private bool CheckEnable()
        {
            if (_stateContext.States.First().IsEnabled == -1)
                return false;

            return true;
        }

        private async Task<SendedMessage> SendMessageAsync(SenderType type, string text, string tgUser, string dbpath, string botKey)
        {
            ChatId destId = null;
            var user = _userContext.Users.SingleOrDefault(
                x => x.Username != null && x.Username.ToLower() == tgUser.ToLower()
                || x.PhoneNumber == _phoneHelper.GetOnlyNumerics(tgUser));
            if (user == null)
            {
                _toFileLogger.LogDebug("До ");
                _logger.LogError($"Не могу отправить сообщение пользователю {tgUser}, т.к.он не добавил бота в телеграмме");
                return null;
            }
            if (type == SenderType.Telegram)
            {
                destId = new ChatId(user.ChatId);
                var bot = new Telegram.Bot.TelegramBotClient(_configService.Config.BotApiKey);
                await bot.SendTextMessageAsync(destId, text);
            }
            else {
                var accountSid = _configService.Config.Twillo.Sid;
                var authToken = _configService.Config.Twillo.Token;

                TwilioClient.Init(accountSid, authToken);

                var message = await MessageResource.CreateAsync(
                    to: new PhoneNumber(user.PhoneNumber),
                    from: new PhoneNumber(_configService.Config.Twillo.PhoneNumber),
                    body: text);
            }
            

            //var bot = new Telegram.Bot.TelegramBotClient(botKey);
            //await bot.SendTextMessageAsync(destId, text);
            return new SendedMessage()
            {
                To = _phoneHelper.Format(user.PhoneNumber),
                Message = $"!(ОТПРАВЛЕНО SMS) {text}"
            };
        }
    }
}
