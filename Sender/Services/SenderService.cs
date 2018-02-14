using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using PayBot.Configuration;
using Sender.Entities;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace Sender.Services
{
    public class SenderService : ISenderService
    {
        private const string _clientSecretPath = "../conf/client_secret.json";
        private const string _credentials = "../conf/credentials";

        private string[] scopes = { SheetsService.Scope.Spreadsheets };
        private const string _applicationName = "Google Sheets API .NET Quickstart";
        private readonly Config _config;

        public SenderService(Config config) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }


        public async Task<bool> Send(CancellationToken cancellation)
        {
            try {
                var service = CreateService();
                var rows = GetData(_config, service);
                if (rows != null) {
                    foreach (var row in rows.OrderByDescending(x => x.LastModifiedDate))
                    {
                        if (row.Status.ToLower() == "надо отправить" && row.MessageSended.ToLower() != "да")
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
            }
            catch (Exception err)
            {

            }
            return await Task.FromResult(true);
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
                //Logging
            }
            return false;
        }

        private async Task<bool> SendMessageAsync(string text, string tgUser, string dbpath, string botKey)
        {
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
            return true;
        }

        private IEnumerable<ValueRow> GetData(Config config, SheetsService service)
        {
            var result = new List<ValueRow>();
            foreach (var spreadsheet in config.Spreadsheets)
            {
                var spreadsheetId = spreadsheet.Id;
                foreach (var list in spreadsheet.Lists)
                {
                    bool allEmpty = false;
                    var rowNum = 2;
                    while(!allEmpty)
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
                        if (date == null && status == null && messageSended == null && text == null && tg == null )
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

            return result;
        }

        private SheetsService CreateService()
        {
            var json = System.IO.File.ReadAllText(_clientSecretPath);
            Secret secret = JsonConvert.DeserializeObject<Secret>(json);

            var googleFlowInitializer = new GoogleAuthorizationCodeFlow.Initializer()
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = secret.installed.client_id,
                    ClientSecret = secret.installed.client_secret
                }
            };
            var token = (new FileDataStore(_credentials, true)).GetAsync<TokenResponse>("user").Result;

            UserCredential credential = new UserCredential(new GoogleAuthorizationCodeFlow(googleFlowInitializer), "user", token);


            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName,
            });
        }
    }
}
