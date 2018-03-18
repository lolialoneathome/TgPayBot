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
        protected readonly ICellService _cellService;
        public SenderService
            (Config config, ISheetsServiceProvider sheetServiceProvider, IBotLogger logger, IPhoneHelper phoneHelper, ILogger<SenderService> toFileLogger,
            ICellService cellService) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sheetServiceProvider = sheetServiceProvider ?? throw new ArgumentNullException(nameof(sheetServiceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _toFileLogger = toFileLogger ?? throw new ArgumentNullException(nameof(toFileLogger));
            _phoneHelper = phoneHelper ?? throw new ArgumentNullException(nameof(phoneHelper));
            _cellService = cellService ?? throw new ArgumentNullException(nameof(cellService));
        }


        public async Task<bool> Send(CancellationToken cancellation)
        {
            try {
                if (!CheckEnable()) {
                    _logger.LogSended($"Рассылка остановлена, ничего не отправляю", null);
                    return false;
                }
                _logger.LogSystem($"Начинаю отправку сообщений...", null);
                var service = _sheetServiceProvider.GetService(); //Need get every times on start for correct token (Но это не точно ¯\_(ツ)_/¯)

                var rows = await GetData(_config, service);
                var sendedMesageCount = 0;
                if (rows != null) {
                    var rowsForUpdate = new Dictionary<string, List<ValueRow>>();
                    var errorList = new List<string>();
                    foreach (var row in rows.OrderBy(x => x.LastModifiedDate))
                    {
                        if (row.Status != null && row.Status.ToLower() == "надо отправить" 
                            && (row.MessageSended == null || row.MessageSended.ToLower() != "да"))
                        {
                            var text = row.MessageText;
                            if (row.TgUser == null)
                            {
                                var rownum = Regex.Match(row.CellForUpdate, @"\d+").Value;
                                errorList.Add
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
                                if (!rowsForUpdate.ContainsKey(row.SheetId))
                                    rowsForUpdate[row.SheetId] = new List<ValueRow>();
                                rowsForUpdate[row.SheetId].Add(row);
                                sendedMesageCount++;
                            }
                        }
                    }
                    _logger.LogErrorList(errorList);
                    if (rowsForUpdate.Count > 0)
                    {
                        foreach (var item in rowsForUpdate) {
                            var updateResult = UpdateTableData(service, item.Key, item.Value);
                        }
                        
                    }
                }
                _logger.LogSystem($"Отправка сообщений закончена. Сообщений отправлено: {sendedMesageCount}", null);
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

        private bool UpdateTableData(SheetsService service, string sheetId, List<ValueRow> rowsForUpdate)
        {
            try {
                var range = new List<string>();
                List<ValueRange> data = new List<ValueRange>();
                foreach (var row in rowsForUpdate) {
                    range.Add($"{row.List}!{row.CellForUpdate}");
                    ValueRange valueRange = new ValueRange();
                    valueRange.Range = $"{row.List}!{row.CellForUpdate}";
                    var oblist = new List<object>() { "да" };
                    valueRange.Values = new List<IList<object>> { oblist };

                    data.Add(valueRange);
                }
                BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest();
                requestBody.ValueInputOption = "RAW";
                requestBody.Data = data;

                SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = service.Spreadsheets.Values.BatchUpdate(requestBody, sheetId);

                request.Execute();

                return true;
            }
            catch (Exception err)
            {
                _logger.LogError($"Произошла непредвиденная ошибка во время обновления данных в таблице {sheetId}! Подробнее: {err.Message} . Stack Trace : {err.StackTrace}");
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

        private async Task<IEnumerable<ValueRow>> GetData(Config config, SheetsService service)
        {
            var result = new List<ValueRow>();
            try {
                foreach (var spreadsheet in config.Spreadsheets)
                {
                    var spreadsheetId = spreadsheet.Id;
                    foreach (var list in spreadsheet.Lists)
                    {

                        var allColumns = new string[] { list.Date, list.Status, list.IsSendedColumn, list.MessageText, list.TgUser };
                        var range = _cellService.GetFullRange(allColumns);
                        var dateIndex = _cellService.GetCellIndex(range, list.Date);
                        var statusIndex = _cellService.GetCellIndex(range, list.Status);
                        var sendedIndex = _cellService.GetCellIndex(range, list.IsSendedColumn);
                        var textIndex = _cellService.GetCellIndex(range, list.MessageText);
                        var tgUserIndex = _cellService.GetCellIndex(range, list.TgUser);

                        SpreadsheetsResource.ValuesResource.GetRequest request = service.Spreadsheets.Values.Get(spreadsheetId, $"{list.ListName}!{range}");
                        request.MajorDimension = SpreadsheetsResource.ValuesResource.GetRequest.MajorDimensionEnum.ROWS;
                        request.DateTimeRenderOption = SpreadsheetsResource.ValuesResource.GetRequest.DateTimeRenderOptionEnum.FORMATTEDSTRING;
                        ValueRange response = request.Execute();

                        var rowNum = 0;
                        foreach (var row in response.Values)
                        {
                            rowNum++;
                            if (rowNum == 1) continue;
                            var strDate = (dateIndex < row.Count()) ? row[dateIndex] : null;
                            var status = (statusIndex < row.Count()) ? row[statusIndex]?.ToString() : null;
                            var messageSended = (sendedIndex < row.Count()) ? row[sendedIndex]?.ToString() : null;
                            var text = (textIndex < row.Count()) ?  row[textIndex]?.ToString() : null;
                            var tg = (tgUserIndex < row.Count()) ?  row[tgUserIndex]?.ToString() : null;

                            DateTime dt;
                            //Get date
                            var formats = new[] { "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss" };
                            DateTime? date = strDate != null &&
                                    DateTime.TryParseExact(strDate.ToString(), formats,
                                        System.Globalization.DateTimeFormatInfo.InvariantInfo,
                                        DateTimeStyles.None,
                                        out dt)
                                ? dt
                                : (DateTime?)null;

                            var item = new ValueRow()
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

                            // If all values empty, we think its end
                            if (strDate != null && date == null && string.IsNullOrEmpty(status)
                                && string.IsNullOrEmpty(messageSended) && string.IsNullOrEmpty(text) && string.IsNullOrEmpty(tg))
                                break;
                            result.Add(item);
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
