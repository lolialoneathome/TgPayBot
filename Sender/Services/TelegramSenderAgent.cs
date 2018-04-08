using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Utils;
using Utils.Logger;

namespace Sender.Services
{
    public class TelegramSenderAgent : ISenderAgent
    {
        protected readonly IConfigService _configService;
        public TelegramSenderAgent(IConfigService configService) {
            _configService = configService;
        }
        public async Task SendMessageAsync(string senderId, string text)
        {
            var bot = new Telegram.Bot.TelegramBotClient(_configService.Config.BotApiKey);
            await bot.SendTextMessageAsync(new ChatId(Convert.ToInt64(senderId)), text);
        }
    }
}
