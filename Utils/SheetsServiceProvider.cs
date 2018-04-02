using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Newtonsoft.Json;
using PayBot.Configuration;
using System.Threading.Tasks;
using System;

namespace Utils
{
    public interface ISheetsServiceProvider {
        Task<SheetsService> GetService();
    }
    public class SheetsServiceProvider : ISheetsServiceProvider
    {
        protected readonly SheetsService _service;
        private readonly string _applicationName;
        private readonly string _clientSecretPath;
        private readonly string _credentialsFolderPath;
        private string[] scopes = { SheetsService.Scope.Spreadsheets };
        

        public SheetsServiceProvider(IConfigService configService, string clientSecretPath, string credsFolderPath) {
            _applicationName = configService.Config.GoogleAppName;
            if (string.IsNullOrEmpty(clientSecretPath))
                throw new ArgumentNullException("Pls, set path to client secret");

            if (string.IsNullOrEmpty(clientSecretPath))
                throw new ArgumentNullException("Pls, set path to creds folder");
            _clientSecretPath = clientSecretPath;
            _credentialsFolderPath = credsFolderPath;
        }

        public async Task<SheetsService> GetService()
        {
            if (_service != null)
                return _service;

            var json = await System.IO.File.ReadAllTextAsync(_clientSecretPath);
            Secret secret = JsonConvert.DeserializeObject<Secret>(json);

            var googleFlowInitializer = new GoogleAuthorizationCodeFlow.Initializer()
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = secret.installed.client_id,
                    ClientSecret = secret.installed.client_secret
                }
            };
            var token = (new FileDataStore(_credentialsFolderPath, true)).GetAsync<TokenResponse>("user").Result;

            UserCredential credential = new UserCredential(new GoogleAuthorizationCodeFlow(googleFlowInitializer), "user", token);

            return new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName,
            });
        }
    }
}
