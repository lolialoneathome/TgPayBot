using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PayBot.Configuration
{
    public class FromFileConfigService : IConfigService
    {
        protected readonly string _path;
        private Config _config;

        public FromFileConfigService(string path) {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("Empty config path");
            _path = path;
            loadConfig();
        }

        public Config Config {
            get {
                if (_config == null)
                    loadConfig();
                return _config;
            }
        }

        public async Task UpdateConfig(Config config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            foreach (var spreedsheet in config.Spreadsheets) {
                if (!IsListValid(spreedsheet.Lists))
                    throw new InvalidOperationException("Invalid LIST params");
            }
            await System.IO.File.WriteAllTextAsync(_path, JsonConvert.SerializeObject(config, Formatting.Indented));
        }

        private void loadConfig() {
            var json = System.IO.File.ReadAllText(_path);
            _config = JsonConvert.DeserializeObject<Config>(json);
        }

        private bool IsListValid(List[] lists) {
            foreach (var list in lists) {
                if (!Regex.IsMatch(list.Date, @"^[a-zA-Z]+$"))
                    return false;
                if (!Regex.IsMatch(list.IsSendedColumn, @"^[a-zA-Z]+$"))
                    return false;
                if (!Regex.IsMatch(list.MessageText, @"^[a-zA-Z]+$"))
                    return false;
                if (!Regex.IsMatch(list.Status, @"^[a-zA-Z]+$"))
                    return false;
                if (!Regex.IsMatch(list.TgUser, @"^[a-zA-Z]+$"))
                    return false;
            }
            return true;
        }
    }
}
