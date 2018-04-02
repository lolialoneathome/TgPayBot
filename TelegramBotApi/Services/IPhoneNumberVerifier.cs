using System.Threading.Tasks;

namespace TelegramBotApi.Services
{
    public interface IPhoneNumberVerifier
    {
        Task<int> SendVerifyRequest(string phone);
    }
}
