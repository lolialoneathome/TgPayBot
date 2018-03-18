namespace PayBot.Configuration
{
    public class Config
    {
        public string BotApiKey { get; set; }
        public string DbPath { get; set; }
        public string GoogleAppName { get; set; }
        public Spreadsheet[] Spreadsheets { get; set; }
        public string[] Admins { get; set; }
        public SpreadsheetLog SpreadsheetLog { get; set; }
        public int SenderTimeout { get; set; }
        public string HelloMessage { get; set; }
        public string AutoresponseText { get; set; }
        public string AlreadySubscribedMessage { get; set; }
        public string UserSubscribed { get; set; }
        public string UserUnsubscribed { get; set; }
        public string UnsupportedMessageType { get; set; }
    }

    public class SpreadsheetLog
    {
        public string Id { get; set; }
        public string Messages { get; set; }
        public string Errors { get; set; }
        public string Auths { get; set; }
    }

    public class Spreadsheet
    {
        public string Id { get; set; }
        public List[] Lists { get; set; }
    }

    public class List
    {
        public string ListName { get; set; }
        public string Date { get; set; }
        public string Status { get; set; }
        public string IsSendedColumn { get; set; }
        public string MessageText { get; set; }
        public string TgUser { get; set; }
    }
}
