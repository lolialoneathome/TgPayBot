using System.Threading.Tasks;

namespace TelegramBotApi.Services
{
    public interface IAdminMessageService
    {
        Task GetUsers(long chatId);

        Task StopSending(long chatId);

        Task StartSending(long chatId);
    }
}
