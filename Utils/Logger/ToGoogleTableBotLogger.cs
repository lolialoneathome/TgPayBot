using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Logging;
using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Utils.Logger
{
    public class ToGoogleTableBotLogger : IBotLogger
    {
        private readonly IConfigService _configService;
        private readonly ISheetsServiceProvider _sheetServiceProvider;
        protected readonly ILogger<ToGoogleTableBotLogger> _toFileLogger;
        public ToGoogleTableBotLogger(IConfigService configService, ISheetsServiceProvider sheetServiceProvider) {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _sheetServiceProvider  = sheetServiceProvider ?? throw new ArgumentNullException(nameof(sheetServiceProvider));
        }

        public async Task LogSended(string action, string user)
        {
            try {
                await LogToTable(_configService.Config.SpreadsheetLog.Messages, action, user, MessageType.Outgoing);
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE. TYPE: SENDED. ACTION: {action} USER: {user} ERROR: {err.Message}");
            }
        }


        public async Task LogAuth(string auth_message, string user)
        {
            try
            {
                await LogToTable(_configService.Config.SpreadsheetLog.Auths, auth_message, user);
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE. TYPE: AUTH. AUTH_MSG: {auth_message} USER: {user} ERROR: {err.Message}");
            }
        }

        public async Task LogError(string error)
        {
            try
            {
                await LogToTable(_configService.Config.SpreadsheetLog.Errors, error);
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE. TYPE: ERROR. ERROR_STR: {error} ERROR: {err.Message}");
            }
        }

        public async Task LogIncoming(string action, string user)
        {
            try
            {
                await LogToTable(_configService.Config.SpreadsheetLog.Messages, action, user, MessageType.Incoming);
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE. TYPE: INCOMING. ERROR_STR: {action} USER: {user} ERROR: {err.Message}");
            }
        }

        public async Task LogSystem(string action, string user)
        {
            try
            {
                await LogToTable(_configService.Config.SpreadsheetLog.Messages, action, user, MessageType.System);
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE. TYPE: SYSTEM. ERROR_STR: {action} USER: {user} ERROR: {err.Message}");
            }
        }

        private async Task LogToTable(string listname, string message, string user = null, MessageType? messageType = null) {
            var service = await _sheetServiceProvider.GetService();

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


            SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(valueRange, _configService.Config.SpreadsheetLog.Id, range);
            request.ValueInputOption = valueInputOption;
            request.InsertDataOption = insertDataOption;
            request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
            AppendValuesResponse response = await request.ExecuteAsync();
        }

        public async Task LogErrorList(IEnumerable<string> errors)
        {
            try {
                var service = await _sheetServiceProvider.GetService();

                var range = $"{_configService.Config.SpreadsheetLog.Errors}!A:B";
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption
                    = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption
                    = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                ValueRange valueRange = new ValueRange();
                var oblist = errors.Select(x => (IList<object>)new List<object>() { string.Format(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")), x }).ToList();
                valueRange.Values = oblist;


                SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(valueRange, _configService.Config.SpreadsheetLog.Id, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;
                request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
                AppendValuesResponse response = request.Execute();
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE ERROR LIST. ERROR: {err.Message}");
            }

        }

        public async Task LogSendedList(IEnumerable<SendedMessage> messages)
        {
            try {
                var service = await _sheetServiceProvider.GetService();

                var range = $"{_configService.Config.SpreadsheetLog.Messages}!A:D";
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption
                    = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption
                    = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                ValueRange valueRange = new ValueRange();

                var listOfObj = messages.Select(x => (IList<object>)new List<object>() { "Исходящее", string.Format(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")), x.Message, x.To }).ToList();
                valueRange.Values = listOfObj;//new List<IList<object>> { listOfObj };


                SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(valueRange, _configService.Config.SpreadsheetLog.Id, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;
                request.ResponseDateTimeRenderOption = SpreadsheetsResource.ValuesResource.AppendRequest.ResponseDateTimeRenderOptionEnum.FORMATTEDSTRING;
                AppendValuesResponse response = request.Execute();
            }
            catch (Exception err)
            {
                _toFileLogger.LogError($"CANNOT LOG TO GOOGLE TABLE SENDED LIST. ERROR: {err.Message}");
            }
        }
    }
}
