using System.Threading.Tasks;

namespace TelegramBotApi.Services
{
    public class FakePhoneNumberVerifier : IPhoneNumberVerifier
    {
        public async Task<int> SendVerifyRequest(string phone)
        {
            return await Task.FromResult(5555);
        }
    }
}
