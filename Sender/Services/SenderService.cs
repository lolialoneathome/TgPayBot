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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sender.Services
{
    public class SenderService : ISenderService
    {
        private const string _configPath = "../conf/config.json";
        private const string _clientSecretPath = "../conf/client_secret.json";
        private const string _credentials = "../conf/credentials";
        private string[] scopes = { SheetsService.Scope.Spreadsheets };
        private const string _applicationName = "Google Sheets API .NET Quickstart";

        public async Task<bool> Send(CancellationToken cancellation)
        {
            var service = CreateService();
            var config = GetConfig();
            var rows = GetData(config, service);

            return await Task.FromResult(true);
        }


        private IEnumerable<ValueRow> GetData(Config config, SheetsService service)
        {
            var result = new List<ValueRow>();
            foreach (var spreadsheet in config.spreadsheets)
            {
                var spreadsheetId = spreadsheet.Id;
                foreach (var list in spreadsheet.Lists)
                {

                    var dateRange = $"{list.listname}!{list.date}2:{list.date}";
                    var statusRange = $"{list.listname}!{list.status}2:{list.date}";
                    var isSendedRange = $"{list.listname}!{list.isSendedColumn}2:{list.date}";
                    var textRange = $"{list.listname}!{list.message_text}2:{list.date}";
                    var tgRange = $"{list.listname}!{list.tg_user}2:{list.date}";

                    SpreadsheetsResource.ValuesResource.BatchGetRequest request =
                        service.Spreadsheets.Values.BatchGet(spreadsheetId);
                    request.MajorDimension = SpreadsheetsResource.ValuesResource.BatchGetRequest.MajorDimensionEnum.COLUMNS;
                    request.Ranges = new string[] { dateRange, statusRange, isSendedRange, textRange, tgRange };


                    BatchGetValuesResponse response = request.Execute();

                    var dict = new Dictionary<int, ValueRow>();
                    var itemCounter = 0;
                    //Get date
                    foreach (var row in response.ValueRanges[0].Values)
                    {
                        dict[itemCounter] = new ValueRow()
                        {
                            LastModifiedDate = Convert.ToDateTime(row[0].ToString())
                        };
                        itemCounter++;
                    }
                    itemCounter = 0;
                    //Get status
                    foreach (var row in response.ValueRanges[1].Values)
                    {
                        dict[itemCounter].Status = row[0].ToString();
                        itemCounter++;
                    }
                    itemCounter = 0;
                    //Get IS sended
                    foreach (var row in response.ValueRanges[2].Values)
                    {
                        dict[itemCounter].IsSended = row[0].ToString().ToLower() == "да";
                        itemCounter++;
                    }
                    itemCounter = 0;
                    //Get message text
                    foreach (var row in response.ValueRanges[3].Values)
                    {
                        dict[itemCounter].MessageText = row[0].ToString();
                        itemCounter++;
                    }
                    itemCounter = 0;
                    //Get tg user
                    foreach (var row in response.ValueRanges[4].Values)
                    {
                        dict[itemCounter].TgUser = row[0].ToString();
                        itemCounter++;
                    }

                    result.AddRange(dict.Select(x => x.Value));
                }
            }

            return result;
        }


        private Config GetConfig()
        {
            var json = System.IO.File.ReadAllText(_configPath);
            return JsonConvert.DeserializeObject<Config>(json);
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
