using System;

namespace Sqllite.Logger
{
    public class LogMessage
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public MessageTypes Type { get; set; }
        public string Text { get; set; }
        public string PhoneNumber { get; set; }
    }
}
