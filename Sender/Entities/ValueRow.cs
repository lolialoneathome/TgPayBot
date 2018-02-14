using System;

namespace Sender.Entities
{
    public class ValueRow
    {
        public DateTime LastModifiedDate { get; set; }
        public string Status { get; set; }
        public bool IsSended { get; set; }
        public string MessageText { get; set; }
        public string TgUser { get; set; }
    }
}