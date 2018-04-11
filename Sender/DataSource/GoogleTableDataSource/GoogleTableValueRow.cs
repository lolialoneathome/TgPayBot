using Sender.DataSource.Base;
using System;

namespace Sender.DataSource.GoogleTabledataSource
{
    public class GoogleTableValueRow : INeedSend
    {
        public DateTime LastModifiedDate { get; set; }
        public string Text { get; set; }
        public string To { get; set; }
        public SenderType SenderType { get; set; }
        public string Table { get; set; }
        public string CellForUpdate { get; set; }
    }
}