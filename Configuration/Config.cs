namespace PayBot.Configuration
{
    public class Config
    {
        public string bot_api_key { get; set; }
        public string db_path { get; set; }
        public Spreadsheet[] spreadsheets { get; set; }
        public string[] admin_users { get; set; }
        public SpreadsheetLog spreadsheet_log { get; set; }
        public int sender_timeout { get; set; }
        public string hello_message { get; set; }
        public string autoresponse { get; set; }
    }

    public class SpreadsheetLog
    {
        public string id { get; set; }
        public string action_list { get; set; }
        public string error_list { get; set; }
        public string auth_list { get; set; }
    }

    public class Spreadsheet
    {
        public string id { get; set; }
        public List[] lists { get; set; }
    }

    public class List
    {
        public string listname { get; set; }
        public string date { get; set; }
        public string status { get; set; }
        public string isSendedColumn { get; set; }
        public string message_text { get; set; }
        public string tg_user { get; set; }
    }
}
