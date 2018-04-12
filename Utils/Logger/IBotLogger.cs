using System.Collections.Generic;
using System.Threading.Tasks;

namespace Utils.Logger
{
    public interface IBotLogger
    {
        Task LogSended(string action, string user);
        Task LogIncoming(string action, string user);
        Task LogSystem(string action, string user);
        Task LogError(string error);
        Task LogErrorList(IEnumerable<string> errors);
        Task LogSendedList(IEnumerable<SendedMessage> errors);
        Task LogAuth(string auth_message, string user);
    }
}
