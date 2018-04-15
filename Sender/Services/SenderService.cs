using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sender.DataSource.Base;
using Sqllite;
using Sqllite.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.DbLogger;
using Utils.Logger;

//TODO! Refactoring require
namespace Sender.Services
{
    public class SenderService : ISenderService
    {
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        private readonly ILogger<SenderService> _toFileLogger;
        protected readonly IDataSource _messageDataSource;

        protected readonly IConfigService _configService;
        protected readonly SqlliteDbContext _dbContext;
        protected readonly ISenderAgentProvider _senderAgentProvider;
        protected readonly INewBotLogger _newLogger;
        public SenderService
            (IConfigService configService,
            IBotLogger logger,
            IPhoneHelper phoneHelper,
            ILogger<SenderService> toFileLogger,
            IDataSource messageDataSource,
            SqlliteDbContext dbContext,
            ISenderAgentProvider senderAgentProvider,
            INewBotLogger newLogger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _messageDataSource = messageDataSource ?? throw new ArgumentNullException(nameof(messageDataSource));
            _senderAgentProvider = senderAgentProvider ?? throw new ArgumentNullException(nameof(senderAgentProvider));
            _newLogger= newLogger ?? throw new ArgumentNullException(nameof(newLogger));
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
                    await _logger.LogSended(LogConst.SendedStopedDoNothing, null);
                    await _newLogger.LogByType(MessageTypes.System, LogConst.SendedStopedDoNothing);
                    return;
                }
                _toFileLogger.LogInformation("Start sending...");
                await _logger.LogSystem(LogConst.StartSending, null);
                await _newLogger.LogByType(MessageTypes.System, LogConst.StartSending);

                if (!IsListValid()) {
                    await _logger.LogError(LogConst.InvalidConfig);
                    await _newLogger.LogByType(MessageTypes.Errors, LogConst.InvalidConfig);
                    return;
                }
                var rows = await _messageDataSource.GetMessages(_config);
                var sendedMesageCount = 0;
                if (rows != null)
                {
                    var rowsForUpdate = new Dictionary<string, List<INeedSend>>();
                    foreach (var message in rows.OrderBy(x => x.LastModifiedDate))
                    {
                        {
                            var text = message.Text;
                            if (message.To == null)
                            {
                                var list = message.CellForUpdate.Substring(0, message.CellForUpdate.IndexOf('!')); // Regex.Match(message.CellForUpdate, @"/^(.*?)\!/").Groups[0];
                                var cellWithoutList = message.CellForUpdate.Substring(message.CellForUpdate.IndexOf('!') + 1, message.CellForUpdate.Length - list.Length - 1);// Regex.Match(message.CellForUpdate, @"/[^!]*$/").Groups[0];
                                var rownum = Regex.Match(cellWithoutList, @"\d+").Value;
                                var errNoPhone = $"У пользователя в таблице {message.Table } на листе {list} в строке {rownum} не указан номер телефона, сообщение НЕ отправлено!";
                                errorList.Add(errNoPhone);
                                await _newLogger.LogByType(MessageTypes.Errors, errNoPhone);
                                continue;
                            }

                            //TODO! Validation to separate service
                            if (_phoneHelper.IsPhone(message.To))
                            {
                                message.To = _phoneHelper.Format(message.To);
                            }

                            if (string.IsNullOrEmpty(message.Text))
                            {
                                var list = message.CellForUpdate.Substring(0, message.CellForUpdate.IndexOf('!')); // Regex.Match(message.CellForUpdate, @"/^(.*?)\!/").Groups[0];
                                var cellWithoutList = message.CellForUpdate.Substring(message.CellForUpdate.IndexOf('!') + 1, message.CellForUpdate.Length - list.Length - 1);// Regex.Match(message.CellForUpdate, @"/[^!]*$/").Groups[0];
                                var rownum = Regex.Match(cellWithoutList, @"\d+").Value;
                                var errEmptyMessage = $"В таблице {message.Table } на листе {list} в строке {rownum} пустое сообщение. Из этой строки ничего не отправляю!";
                                errorList.Add(errEmptyMessage);
                                await _newLogger.LogByType(MessageTypes.Errors, errEmptyMessage);
                                continue;
                            }

                            var senderAgent = _senderAgentProvider.Resolve(message.SenderType);
                            var sendResult = await senderAgent.Send(message);

                            if (sendResult.IsSuccess)
                            {
                                sendedList.Add(new SendedMessage() {
                                    Message = message.Text,
                                    To = message.To
                                });
                                await _newLogger.LogByType(MessageTypes.Outgoing, $"На {message.SenderType} : {message.Text}", _phoneHelper.Clear(message.To));
                                if (!rowsForUpdate.ContainsKey(message.Table))
                                    rowsForUpdate[message.Table] = new List<INeedSend>();
                                rowsForUpdate[message.Table].Add(message);
                                sendedMesageCount++;
                            }
                            else
                            {
                                var sendErr = $"Не удалось отправить сообщение пользователю {message.To}. Ошибка: {sendResult.Error}";
                                errorList.Add(sendErr);
                                await _newLogger.LogByType(MessageTypes.Errors, sendErr);
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
                    await _logger.LogErrorList(errorList);
                    isErrorSended = true;
                    await _logger.LogSendedList(sendedList);
                    isSuccessSended = true;

                }
                var sendingEnded = $"Отправка сообщений закончена. Сообщений отправлено: {sendedMesageCount}";
                await _logger.LogSystem(sendingEnded, null);
                await _newLogger.LogByType(MessageTypes.System, sendingEnded);
                _toFileLogger.LogInformation($"End sending. Message count: {sendedMesageCount}");
            }
            catch (Exception err)
            {
                _toFileLogger.LogError(err, err.Message);
                if (!isErrorSended)
                    await _logger.LogErrorList(errorList);
                if (!isSuccessSended)
                    await _logger.LogSendedList(sendedList);
                var errToLog = $"Произошла непредвиденная ошибка во время отправки сообщений! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}";
                await _newLogger.LogByType(MessageTypes.SystemErrors, errToLog);
                await _logger.LogError(errToLog);
            }
        }

        private bool CheckEnable()
        {
            var state = _dbContext.States.SingleOrDefault();
            _dbContext.ReloadEntity(state);
            if (state.IsEnabled == -1)
                return false;

            return true;
        }
    }
}
