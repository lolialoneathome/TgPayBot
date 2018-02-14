using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using PayBot.Configuration;
using Sqllite;
using System;
using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types;

namespace PayBotServiceApp
{
    class Program
    {
        static string _botApiKey;

        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static string ApplicationName = "Google Sheets API .NET Quickstart";

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Message sender started, pls do not close this window!");

                var service = CreateService();

                var json = System.IO.File.ReadAllText("config.json");
                Config configuration = JsonConvert.DeserializeObject<Config>(json);
                _botApiKey = configuration.BotApiKey;

                var messageCount = 0;

                foreach (var spreadsheet in configuration.Spreadsheets)
                {
                    var spreadsheetId = spreadsheet.Id;
                    foreach (var list in spreadsheet.Lists)
                    {
                        var range = $"{list.ListName}!{list.start_column}:{list.end_column}";
                        SpreadsheetsResource.ValuesResource.GetRequest request =
                            service.Spreadsheets.Values.Get(spreadsheetId, range);

                        ValueRange response = request.Execute();

                        var rowNum = 0;
                        foreach (var row in response.Values)
                        {
                            rowNum++;
                            if (row.Count < 2)
                                continue;
                            var status = row[0].ToString();
                            var isSended = row[1].ToString();
                            if (status.ToLower() == "надо отправить" && isSended.ToLower() != "да")
                            {
                                var text = row[2].ToString();
                                var tgUser = row[3].ToString();

                                var sendMessageResult = SendMessageAsync(text, tgUser, configuration.DbPath).Result;
                                if (sendMessageResult)
                                {
                                    messageCount++;
                                    UpdateTableData(service, spreadsheetId, rowNum, list.IsSendedColumn, list.ListName);
                                }

                            }
                        }
                    }
                }

                Console.WriteLine("Work ended. Sended {0} messages. Press enter to close message sender", messageCount);
            }
            catch (Exception err)
            {
                Console.WriteLine($"Fatal exception {err.Message}. Press enter to close...");
            }
            
            Console.ReadLine();
        }

        private static void UpdateTableData(SheetsService service, string spreadsheetId, int rowNum, string column, string listname)
        {
            var range = $"{listname}!{column}{rowNum}";
            ValueRange valueRange = new ValueRange();
            valueRange.MajorDimension = "COLUMNS";//"ROWS";//COLUMNS

            var oblist = new List<object>() { "да" };
            valueRange.Values = new List<IList<object>> { oblist };

            SpreadsheetsResource.ValuesResource.UpdateRequest update = service.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            UpdateValuesResponse result2 = update.Execute();
        }

        private static SheetsService CreateService() {

            var json = System.IO.File.ReadAllText("client_secret.json");
            Secret secret = JsonConvert.DeserializeObject<Secret>(json);


            var credPath = "credentials";
            var googleFlowInitializer = new GoogleAuthorizationCodeFlow.Initializer()
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = secret.installed.client_id,
                    ClientSecret = secret.installed.client_secret
                }
            };
            var token = (new FileDataStore(credPath, true)).GetAsync<TokenResponse>("user").Result;

            UserCredential credential = new UserCredential(new GoogleAuthorizationCodeFlow(googleFlowInitializer), "user", token);


            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private async static System.Threading.Tasks.Task<bool> SendMessageAsync(string text, string tgUser, string dbpath)
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
            var bot = new Telegram.Bot.TelegramBotClient(_botApiKey);
            await bot.SendTextMessageAsync(destId, text);
            Console.WriteLine("Отправил сообщение пользователю {0}", tgUser);
            return true;
        }
    }
}
