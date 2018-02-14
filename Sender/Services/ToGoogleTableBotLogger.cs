﻿using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Logger;
using Newtonsoft.Json;
using PayBot.Configuration;
using System;
using System.Collections.Generic;

namespace Sender.Services
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
            LogToTable(_config.SpreadsheetLog.Sended, action, user);
        }


        public void LogAuth(string auth_message)
        {
            LogToTable(_config.SpreadsheetLog.Auths, auth_message);
        }

        public void LogError(string error)
        {
            LogToTable(_config.SpreadsheetLog.Errors, error);
        }

        public void LogIncoming(string action, string user)
        {
            LogToTable(_config.SpreadsheetLog.Incoming, action, user);
        }


        private void LogToTable(string listname, string message, string user = null) {
            try
            {
                var service = _sheetServiceProvider.GetService();

                var range = $"{listname}!A:D";
                SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum valueInputOption 
                    = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum insertDataOption 
                    = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
                ValueRange valueRange = new ValueRange();

                var oblist = new List<object>() { DateTime.Now, message, user };
                valueRange.Values = new List<IList<object>> { oblist };


                SpreadsheetsResource.ValuesResource.AppendRequest request = service.Spreadsheets.Values.Append(valueRange, _config.SpreadsheetLog.Id, range);
                request.ValueInputOption = valueInputOption;
                request.InsertDataOption = insertDataOption;

                AppendValuesResponse response = request.Execute();
            }
            catch (Exception err)
            {
                //Logging ??? AAAAAAAA
            }
        }

    }
}