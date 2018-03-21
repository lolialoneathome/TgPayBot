using System.Threading.Tasks;

namespace TelegramListener.Services
{
    public interface IPhoneNumberVerifier
    {
        Task<int> SendVerifyRequest(string phone);
    }
}
