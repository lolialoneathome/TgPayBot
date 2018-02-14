namespace Logger
{
    public interface IBotLogger
    {
        void LogSended(string action, string user);
        void LogIncoming(string action, string user);
        void LogError(string error);
        void LogAuth(string auth_message);
    }
}
