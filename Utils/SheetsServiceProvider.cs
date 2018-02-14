using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using PayBot.Configuration;

namespace Utils
{
    public interface ISheetsServiceProvider {
        SheetsService GetService();
    }
    public class SheetsServiceProvider : ISheetsServiceProvider
    {
        protected readonly SheetsService _service;
        private const string _clientSecretPath = "../conf/client_secret.json";
        private const string _credentials = "../conf/credentials";

        private string[] scopes = { SheetsService.Scope.Spreadsheets };
        private const string _applicationName = "Google Sheets API .NET Quickstart";
        public SheetsService GetService()
        {
            if (_service != null)
                return _service;

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
