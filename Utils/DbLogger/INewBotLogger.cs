using Sqllite.Logger;
using System.Threading.Tasks;

namespace Utils.DbLogger
{
    public interface INewBotLogger
    {
        Task LogByType(MessageTypes type, string text, string person = null); // Temporarily here
        Task<LogMessageStatus> LogAuth(string auth_message, string user);
        Task<LogMessageStatus> LogOutgoing(string action, string user);
        Task<LogMessageStatus> LogIncoming(string action, string user);
        Task<LogMessageStatus> LogSystem(string action, string user);
        Task<LogMessageStatus> LogError(string error);
        Task<LogMessageStatus> LogSystemError(string error);
    }
}
