namespace Sqllite.Logger
{
    public enum MessageTypes
    {
        Auth, // Авторизации
        Outgoing, // Исходящие
        Incoming, // Входящие
        Errors, // Ошибки во время отправки
        SystemErrors, // Какие-то системные ошибки
        System // Системные сообщения (вкл/выкл например)
    }
}
