using PayBot.Configuration;
using Sender.DataSource.Base;
using Sqllite;
using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Utils;

namespace Sender.DataSource.SenderAgents
{
    public class TelegramSenderAgent : ISenderAgent
    {
        protected readonly IConfigService _configService;
        protected readonly SqlliteDbContext _dbContext;
        protected readonly IPhoneHelper _phoneHelper;
        public TelegramSenderAgent(IConfigService configService, SqlliteDbContext dbContext, IPhoneHelper phoneHelper) {
            _configService = configService ?? throw new ArgumentNullException();
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _phoneHelper = phoneHelper;
    }

        public async Task<MessageSendResult> Send(INeedSend message)
        {
            ChatId destId = null;
            var user = _dbContext.Users.SingleOrDefault(
                x => x.Username != null && x.Username.ToLower() == message.To.ToLower()
                || x.PhoneNumber == _phoneHelper.Clear(message.To.ToLower()));
            if (user == null)
            {
                return new MessageSendResult()
                {
                    Message = message,
                    IsSuccess = false,
                    Error = $"Не могу отправить сообщение пользователю {message.To}, т.к.он не добавил бота в телеграмме"
                };
            }
            destId = new ChatId(user.ChatId);
            var bot = new Telegram.Bot.TelegramBotClient(_configService.Config.BotApiKey);
            await bot.SendTextMessageAsync(destId, message.Text);
            return new MessageSendResult()
            {
                Message = message,
                IsSuccess = true
            };
        }
    }
}
