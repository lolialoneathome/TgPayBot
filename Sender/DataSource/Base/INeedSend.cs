using System;

namespace Sender.DataSource.Base
{
    public interface INeedSend
    {
        DateTime LastModifiedDate { get; set; }
        string Text { get; set; }
        string To { get; set; }
        SenderType SenderType { get; set; }
        string Table { get; set; }
        string CellForUpdate { get; set; }
    }
}
