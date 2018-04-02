using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace PayBot.Configuration
{
    public class FromFileConfigService : IConfigService
    {
        public readonly Config _config;

        public FromFileConfigService(string path) {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("Empty config path");
            var json = System.IO.File.ReadAllText(path);
            _config = JsonConvert.DeserializeObject<Config>(json);
        }

        public Config Config => _config;

        public void UpdateConfig(Config config)
        {
            throw new NotImplementedException();
        }
    }
}
