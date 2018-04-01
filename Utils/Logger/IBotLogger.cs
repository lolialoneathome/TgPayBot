using System.Collections.Generic;

namespace Utils.Logger
{
    public interface IBotLogger
    {
        void LogSended(string action, string user);
        void LogIncoming(string action, string user);
        void LogSystem(string action, string user);
        void LogError(string error);
        void LogErrorList(IEnumerable<string> errors);
        void LogSendedList(IEnumerable<SendedMessage> errors);
        void LogAuth(string auth_message, string user);
    }
}
