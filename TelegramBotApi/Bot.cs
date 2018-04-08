using PayBot.Configuration;
using System;
using Telegram.Bot;

namespace TelegramBotApi
{
    public class Bot
    {
        public static TelegramBotClient Api;
        public Bot(IConfigService configService) {
            ApiKey = configService.Config.BotApiKey;
            Api = new TelegramBotClient(ApiKey);
        }
        public static string ApiKey { get; set; }
    }
}