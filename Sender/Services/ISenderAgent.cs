using System.Threading.Tasks;
using Utils.Logger;

namespace Sender.Services
{
    public interface ISenderAgent
    {
        Task SendMessageAsync(string senderId, string text);
    }
}
