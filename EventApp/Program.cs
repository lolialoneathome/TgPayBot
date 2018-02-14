using Newtonsoft.Json;
using System;
using System.IO;

namespace EventApp
{
    class Program
    {
        static void Main(string[] args)
        {
            try {
                var json = File.ReadAllText("config.json");
                Config config = JsonConvert.DeserializeObject<Config>(json);

                var eventClient = new EventsTelegramBotClient(config.bot_api_key, config.db_path);

                eventClient.StartReceiving();
                //while (true)
                //{

                //}
                Console.WriteLine("Event listener has started now. Press enter if need stop it");
                Console.ReadLine();
                eventClient.StopReceiving();
            }
            catch (Exception err)
            {
                Console.WriteLine($"Fatal exception {err.Message}. Press enter to close...");
            }

        }
    }
}
