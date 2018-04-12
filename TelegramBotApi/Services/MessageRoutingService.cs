using System;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramBotApi.Services
{
    public class MessageRoutingService : IMessageRoutingService
    {
        protected readonly IUserMessageService _userMessageService;
        protected readonly IAdminMessageService _adminMessageService;
        public MessageRoutingService(IUserMessageService messageService, IAdminMessageService adminMessageService) {
            _userMessageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _adminMessageService = adminMessageService ?? throw new ArgumentNullException(nameof(adminMessageService));
        }
        public async Task RouteContactMessage(Message message)
        {
            await _userMessageService.ReceivedContact(message.Chat.Id, message.From.Username, message.Contact.PhoneNumber);
        }

        public async Task RouteTextMessage(Message message)
        {
            var chatId = message.Chat.Id;
            switch (message.Text)
            {
                case "/start":
                    await _userMessageService.RequestSubscribe(chatId, message.From.Username);
                    break;
                case "/bye":
                    await _userMessageService.Unsubscribe(chatId, message.From.Username);
                    break;
                case "Отправить код ещё раз":
                    await _userMessageService.ResetCode(chatId);
                    break;
                case "/get_users":
                    await _adminMessageService.GetUsers(chatId);
                    break;
                case "/start_sending":
                    await _adminMessageService.StartSending(chatId);
                    break;
                case "/stop_sending":
                    await _adminMessageService.StopSending(chatId);
                    break;
                default:
                    await _userMessageService.ReceiveTextMessage(chatId, message.Text, message.From.Username);
                    break;
            }
        }

        public async Task RouteUnsopportedMessage(Message message)
        {
            await _userMessageService.ReceiveUnsupportedMessage(message.Chat.Id, message.From.Username);
        }
    }
}
