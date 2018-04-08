using System;

namespace Sender.DataSource.Base
{
    public interface IMessage
    {
        DateTime LastModifiedDate { get; set; }
        string Status { get; set; }
        string IsMessageAlreadySended { get; set; }
        string Text { get; set; }
        string To { get; set; }
        string Table { get; set; }
        string CellForUpdate { get; set; }
        SenderType SenderType { get; set; }
    }
}
