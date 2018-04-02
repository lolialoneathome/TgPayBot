using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using PayBot.Configuration;
using Sender.DataSource.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Utils;

namespace Sender.DataSource.GoogleTabledataSource
{
    public class GoogleTableDataSource : IMessageDataSource
    {
        protected readonly ICellService _cellService;
        protected readonly ISheetsServiceProvider _sheetServiceProvider;
        public GoogleTableDataSource(ICellService cellService, ISheetsServiceProvider sheetServiceProvider) {
            _cellService = cellService ?? throw new ArgumentNullException(nameof(cellService));
            _sheetServiceProvider = sheetServiceProvider ?? throw new ArgumentNullException(nameof(sheetServiceProvider));
        }

        public async Task<IEnumerable<IMessage>> GetMessages(Config config)
        {
            var service = await _sheetServiceProvider.GetService();
            var result = new List<GoogleTableValueRow>();
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
                        var text = (textIndex < row.Count()) ? row[textIndex]?.ToString() : null;
                        var tg = (tgUserIndex < row.Count()) ? row[tgUserIndex]?.ToString() : null;

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

                        var item = new GoogleTableValueRow()
                        {
                            LastModifiedDate = date == null ? DateTime.MinValue : (DateTime)date,
                            Status = status,
                            IsMessageAlreadySended = messageSended,
                            Text = text,
                            To = tg,
                            Table = spreadsheetId,
                            CellForUpdate = $"{list.ListName}!{list.IsSendedColumn}{rowNum}"
                        };

                        // If all values empty, we think its end
                        if (strDate != null && date == null && string.IsNullOrEmpty(status)
                            && string.IsNullOrEmpty(messageSended) && string.IsNullOrEmpty(text) && string.IsNullOrEmpty(tg))
                            break;

                        if (item.Status != null && item.Status.ToLower() == "надо отправить"
                            && (item.IsMessageAlreadySended == null || item.IsMessageAlreadySended.ToLower() != "да")) {
                            result.Add(item);
                        }
                            
                    }
                }
            }
            return result;
        }

        public async Task<bool> UpdateMessageStatus(IEnumerable<IMessage> messages)
        {
            List<ValueRange> data = new List<ValueRange>();
            foreach (var row in messages)
            {
                ValueRange valueRange = new ValueRange();
                valueRange.Range = row.CellForUpdate;
                var oblist = new List<object>() { "да" };
                valueRange.Values = new List<IList<object>> { oblist };

                data.Add(valueRange);
            }
            BatchUpdateValuesRequest requestBody = new BatchUpdateValuesRequest();
            requestBody.ValueInputOption = "RAW";
            requestBody.Data = data;

            var service = await _sheetServiceProvider.GetService();
            SpreadsheetsResource.ValuesResource.BatchUpdateRequest request = service.Spreadsheets.Values.BatchUpdate(requestBody, messages.First().Table);

            request.Execute();

            return true;
        }
    }
}
