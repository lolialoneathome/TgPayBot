using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace TelegramBotApi.Services
{
    public interface IMessageRoutingService
    {
        Task RouteTextMessage(Message message);
        Task RouteContactMessage(Message message);
        Task RouteUnsopportedMessage(Message message);
    }
}
