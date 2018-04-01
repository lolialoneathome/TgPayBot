using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sender.DataSource.Base;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Utils;
using Utils.Logger;

namespace Sender.Services
{
    public class SenderService : ISenderService
    {
        private readonly Config _config;
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        private readonly ILogger<SenderService> _toFileLogger;
        protected readonly IMessageDataSource _messageDataSource;
        public SenderService
            (Config config, IBotLogger logger, IPhoneHelper phoneHelper, ILogger<SenderService> toFileLogger, IMessageDataSource messageDataSource) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
  
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _messageDataSource = messageDataSource ?? throw new ArgumentNullException(nameof(messageDataSource));
        }


        public async Task Process(CancellationToken cancellation)
        {
            var errorList = new List<string>();
            var sendedList = new List<SendedMessage>();
            var isErrorSended = false;
            var isSuccessSended = false;
            try {
                if (!CheckEnable()) {
                    _toFileLogger.LogInformation("Sendidng stoped. Do nothing.");
                    _logger.LogSended($"Рассылка остановлена, ничего не отправляю", null);
                    return;
                }
                _toFileLogger.LogInformation("Start sending...");
                _logger.LogSystem($"Начинаю отправку сообщений...", null);

                var rows = await _messageDataSource.GetMessages();
                var sendedMesageCount = 0;
                if (rows != null) {
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
                            var sendMessageResult = await SendMessageAsync(text, phone, _config.DbPath, _config.BotApiKey);
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
            using (var states = new StateContext(_config.DbPath))
            {
                if (states.States.First().IsEnabled == -1)
                    return false;

                return true;
            }
        }

        private async Task<SendedMessage> SendMessageAsync(string text, string tgUser, string dbpath, string botKey)
        {
            ChatId destId = null;
            using (var db = new UserContext(dbpath))
            {
                var user = db.Users.SingleOrDefault(
                    x => x.Username != null && x.Username.ToLower() == tgUser.ToLower()
                    || x.PhoneNumber == _phoneHelper.GetOnlyNumerics(tgUser));
                if (user == null)
                {
                    _toFileLogger.LogDebug("До ");
                    _logger.LogError($"Не могу отправить сообщение пользователю {tgUser}, т.к.он не добавил бота в телеграмме");
                    return null;
                }
                destId = new ChatId(user.ChatId);

                var bot = new Telegram.Bot.TelegramBotClient(botKey);
                await bot.SendTextMessageAsync(destId, text);
                return new SendedMessage() {
                    To = _phoneHelper.Format(user.PhoneNumber),
                    Message = text
                };
            }
        }
    }
}
