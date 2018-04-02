using Sqllite;
using System.Threading.Tasks;

namespace TelegramBotApi.Services
{
    public interface IUserMessageService
    {
        Task RequestSubscribe(long chatId, string username);
        Task ReceivedContact(long chatId, string username, string phone);
        Task Unsubscribe(long chatId, string username);
        Task ReceiveTextMessage(long chatId, string text, string username);
        Task ReceiveVerifyPhonenumbercode(UnauthorizedUser unauthUser, string text);
        Task ReceiveUnsupportedMessage(long chatId, string from);
        Task ResetCode(long chatId);
    }
}
