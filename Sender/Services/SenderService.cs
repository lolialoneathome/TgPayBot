using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using PayBot.Configuration;
using Sender.Entities;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public SenderService(Config config, ISheetsServiceProvider sheetServiceProvider, IBotLogger logger) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sheetServiceProvider = sheetServiceProvider ?? throw new ArgumentNullException(nameof(sheetServiceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<bool> Send(CancellationToken cancellation)
        {
            try {
                if (!CheckEnable()) {
                    _logger.LogSended($"Рассылка остановлена, ничего не отправляю", null);
                    return false;
                } 

                var service = _sheetServiceProvider.GetService(); //Need get every times on start for correct token (Но это не точно ¯\_(ツ)_/¯)
                var rows = GetData(_config, service);
                if (rows != null) {
                    foreach (var row in rows.OrderByDescending(x => x.LastModifiedDate))
                    {
                        if (row.Status != null && row.Status.ToLower() == "надо отправить" 
                            && (row.MessageSended == null || row.MessageSended.ToLower() != "да"))
                        {
                            var text = row.MessageText;
                            var tgUser = row.TgUser;

                            var sendMessageResult = await SendMessageAsync(text, tgUser, _config.DbPath, _config.BotApiKey);
                            if (sendMessageResult)
                            {
                                var updateResult = UpdateTableData(service, row.SheetId, row.List, row.CellForUpdate);
                            }
                        }
                    }
                }
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
                    var user = db.Users.SingleOrDefault(x => x.Username.ToLower() == tgUser.ToLower());
                    if (user == null)
                    {
                        Console.WriteLine("Не могу отправить сообщение пользователю {0}, т.к. он не добавил бота в телеграмме", tgUser);
                        return false;
                    }
                    destId = new ChatId(user.ChatId);
                }
                var bot = new Telegram.Bot.TelegramBotClient(botKey);
                await bot.SendTextMessageAsync(destId, text);

                _logger.LogSended($"Отправлено ообщение с текстом {text}", tgUser);

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
                            //Get date
                            DateTime? date = response.ValueRanges[0].Values?[0]?[0] != null
                                ? Convert.ToDateTime(response.ValueRanges[0].Values?[0]?[0])
                                : (DateTime?)null;
                            var status = response.ValueRanges[1].Values?[0]?[0]?.ToString();
                            var messageSended = response.ValueRanges[2].Values?[0]?[0]?.ToString();
                            var text = response.ValueRanges[3].Values?[0]?[0]?.ToString();
                            var tg = response.ValueRanges[4].Values?[0]?[0]?.ToString();
                            if (date == null && status == null && messageSended == null && text == null && tg == null)
                            {
                                allEmpty = true;
                                continue;
                            };
                            var row = new ValueRow()
                            {
                                LastModifiedDate = (DateTime)date,
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
