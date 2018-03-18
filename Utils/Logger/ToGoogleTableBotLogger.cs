using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Utils.Logger
{
    public class ToGoogleTableBotLogger : IBotLogger
    {
        private readonly Config _config;
        private readonly ISheetsServiceProvider _sheetServiceProvider;

        public ToGoogleTableBotLogger(Config config, ISheetsServiceProvider sheetServiceProvider) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _sheetServiceProvider  = sheetServiceProvider ?? throw new ArgumentNullException(nameof(sheetServiceProvider));
        }

        public void LogSended(string action, string user)
        {
            LogToTable(_config.SpreadsheetLog.Messages, action, user, MessageType.Outgoing);
        }


        public void LogAuth(string auth_message, string user)
        {
            LogToTable(_config.SpreadsheetLog.Auths, auth_message, user);
        }

        public void LogError(string error)
        {
            LogToTable(_config.SpreadsheetLog.Errors, error);
        }

        public void LogIncoming(string action, string user)
        {
            LogToTable(_config.SpreadsheetLog.Messages, action, user, MessageType.Incoming);
        }

        public void LogSystem(string action, string user)
        {
            LogToTable(_config.SpreadsheetLog.Messages, action, user, MessageType.System);
        }

        private void LogToTable(string listname, string message, string user = null, MessageType? messageType = null) {
            try
            {
                var service = _sheetServiceProvider.GetService();

                var range = $"{listname}!A:D";
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption 
                    = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption 
                    = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                ValueRange valueRange = new ValueRange();
                var oblist = new List<object>();
                if (messageType != null)
                    oblist.Add(
                        messageType == MessageType.Outgoing ? "Исходящее" :
                        messageType == MessageType.Incoming ? "Входящее"
                        : "Системное"
                        );
                oblist.AddRange(new List<object>() { string.Format(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")), message, user});

                valueRange.Values = new List<IList<object>> { oblist };


                SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(valueRange, _config.SpreadsheetLog.Id, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;
                request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
                AppendValuesResponse response = request.Execute();
            }
            catch (Exception err)
            {
                //Logging ??? AAAAAAAA ????

                Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAA " + err.Message);
            }
        }

        public void LogErrorList(IEnumerable<string> errors)
        {
            try
            {
                var service = _sheetServiceProvider.GetService();

                var range = $"{_config.SpreadsheetLog.Errors}!A:B";
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption
                    = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption
                    = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                ValueRange valueRange = new ValueRange();
                var oblist = new List<object>();

                //oblist.AddRange(new List<object>() { string.Format(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")), message, user });
                oblist.AddRange(errors.Select(x => new List<object>() { string.Format(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")), x}));
                valueRange.Values = new List<IList<object>> { oblist };


                SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(valueRange, _config.SpreadsheetLog.Id, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;
                request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
                AppendValuesResponse response = request.Execute();
            }
            catch (Exception err)
            {
                //Logging ??? AAAAAAAA ????

                Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAA " + err.Message);
            }
        }
    }
}
