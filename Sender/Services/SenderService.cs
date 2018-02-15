using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using Sender.Entities;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly ISheetsServiceProvider _sheetServiceProvider;
        protected readonly IBotLogger _logger;
        protected readonly IPhoneHelper _phoneHelper;
        private readonly ILogger<SenderService> _toFileLogger;
        public SenderService
            (Config config, ISheetsServiceProvider sheetServiceProvider, IBotLogger logger, IPhoneHelper phoneHelper, ILogger<SenderService> toFileLogger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sheetServiceProvider = sheetServiceProvider ?? throw new ArgumentNullException(nameof(sheetServiceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
        }


        public async Task<bool> Send(CancellationToken cancellation)
        {
            try {
                if (!CheckEnable()) {
                    _logger.LogSended($"Рассылка остановлена, ничего не отправляю", null);
                    return false;
                }
                _logger.LogSended($"Начинаю отправку сообщений...", null);
                var service = _sheetServiceProvider.GetService(); //Need get every times on start for correct token (Но это не точно ¯\_(ツ)_/¯)

                var rows = GetData(_config, service);
                var sendedMesageCount = 0;
                if (rows != null) {
                    foreach (var row in rows.OrderBy(x => x.LastModifiedDate))
                    {
                        if (row.Status != null && row.Status.ToLower() == "надо отправить" 
                            && (row.MessageSended == null || row.MessageSended.ToLower() != "да"))
                        {
                            var text = row.MessageText;
                            if (row.TgUser == null)
                            {
                                var rownum = Regex.Match(row.CellForUpdate, @"\d+").Value;
                                _logger.LogError
                                    ($"У пользователя в таблице {row.SheetId } на листе {row.List} в строке {rownum} не указан номер телефона, сообщение НЕ отправлено!");
                                continue;
                            }

                            if (_phoneHelper.IsPhone(row.TgUser))
                            {
                                row.TgUser = _phoneHelper.Format(row.TgUser);
                            }
                            var tgUser = row.TgUser;
                            var sendMessageResult = await SendMessageAsync(text, tgUser, _config.DbPath, _config.BotApiKey);
                            if (sendMessageResult)
                            { 
                                var updateResult = UpdateTableData(service, row.SheetId, row.List, row.CellForUpdate);
                                sendedMesageCount++;
                            }
                        }
                    }
                }
                _logger.LogSended($"Отправка сообщений закончена. Сообщений отправлено: {sendedMesageCount}", null);
                return await Task.FromResult(true);
            }
            catch (Exception err)
            {
                _logger.LogError($"Произошла непредвиденная ошибка во время отправки сообщений! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}");
            }
            return await Task.FromResult(false);
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

        private bool UpdateTableData(SheetsService service, string sheetId, string list, string cellForUpdate)
        {
            try {
                var range = $"{list}!{cellForUpdate}";
                ValueRange valueRange = new ValueRange();

                var oblist = new List<object>() { "да" };
                valueRange.Values = new List<IList<object>> { oblist };

                SpreadsheetsResource.ValuesResource.UpdateRequest update = service.Spreadsheets.Values.Update(valueRange, sheetId, range);
                update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                UpdateValuesResponse result = update.Execute();
                return true;
            }
            catch (Exception err)
            {
                _logger.LogError($"Произошла непредвиденная ошибка во время обновления данных в таблице на листе {list} в ячейке {cellForUpdate}! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}");
            }
            return false;
        }

        private async Task<bool> SendMessageAsync(string text, string tgUser, string dbpath, string botKey)
        {
            try {
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
                        return false;
                    }
                    destId = new ChatId(user.ChatId);

                    var bot = new Telegram.Bot.TelegramBotClient(botKey);
                    await bot.SendTextMessageAsync(destId, text);
                    _logger.LogSended($"{text}", _phoneHelper.Format(user.PhoneNumber));
                }
                return true;
            }
            catch(Exception err)
            {
                _logger.LogError($"Произошла непредвиденная ошибка во время отправки сообщения [{text}] пользоватею {tgUser}! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}");
            }
            return false;
        }

        private IEnumerable<ValueRow> GetData(Config config, SheetsService service)
        {
            var result = new List<ValueRow>();
            try {
                foreach (var spreadsheet in config.Spreadsheets)
                {
                    var spreadsheetId = spreadsheet.Id;
                    foreach (var list in spreadsheet.Lists)
                    {
                        bool allEmpty = false;
                        var rowNum = 2;
                        while (!allEmpty)
                        {
                            var dateRange = $"{list.ListName}!{list.Date}{rowNum}";
                            var statusRange = $"{list.ListName}!{list.Status}{rowNum}";
                            var isSendedRange = $"{list.ListName}!{list.IsSendedColumn}{rowNum}";
                            var textRange = $"{list.ListName}!{list.MessageText}{rowNum}";
                            var tgRange = $"{list.ListName}!{list.TgUser}{rowNum}";

                            SpreadsheetsResource.ValuesResource.BatchGetRequest request =
                                service.Spreadsheets.Values.BatchGet(spreadsheetId);
                            request.Ranges = new string[] { dateRange, statusRange, isSendedRange, textRange, tgRange };

                            BatchGetValuesResponse response = request.Execute();
                            var dict = new Dictionary<int, ValueRow>();

                            var strDate = response.ValueRanges[0].Values?[0]?[0]; //Костыль для linux
                            DateTime dt;
                            //Get date
                            var formats = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH-mm-ss", "yyyy-MM-dd", "yyyy-MM-dd HH-mm-ss" };
                            DateTime? date = response.ValueRanges[0].Values?[0]?[0] != null && 
                                    DateTime.TryParseExact(response.ValueRanges[0].Values?[0]?[0].ToString(), formats,
                                        new CultureInfo("en-US"),
                                        DateTimeStyles.None,
                                        out dt)
                                ? dt
                                : (DateTime?)null;
                            var status = response.ValueRanges[1].Values?[0]?[0]?.ToString();
                            var messageSended = response.ValueRanges[2].Values?[0]?[0]?.ToString();
                            var text = response.ValueRanges[3].Values?[0]?[0]?.ToString();
                            var tg = response.ValueRanges[4].Values?[0]?[0]?.ToString();
                            


                            if (strDate == null && date == null && status == null && messageSended == null && text == null && tg == null)
                            {
                                allEmpty = true;
                                continue;
                            };
                            var row = new ValueRow()
                            {
                                LastModifiedDate = date == null ? DateTime.MinValue : (DateTime)date,
                                Status = status,
                                MessageSended = messageSended,
                                MessageText = text,
                                TgUser = tg,
                                SheetId = spreadsheetId,
                                List = list.ListName,
                                CellForUpdate = $"{list.IsSendedColumn}{rowNum}"
                            };
                            result.Add(row);
                            rowNum++;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                _logger.LogError($"Произошла непредвиденная ошибка во время получения данных! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}");
            }
            return result;
        }
    }
}
