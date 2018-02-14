using System;

namespace Sender.Entities
{
    public class ValueRow
    {
        public DateTime LastModifiedDate { get; set; }
        public string Status { get; set; }
        public string MessageSended { get; set; }
        public string MessageText { get; set; }
        public string TgUser { get; set; }

        public string SheetId {get; set;}
        public string List { get; set; }
        public string CellForUpdate { get; set; }
        
    }
}