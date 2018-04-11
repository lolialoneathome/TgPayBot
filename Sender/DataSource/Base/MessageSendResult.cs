using Utils.Logger;

namespace Sender.DataSource.Base
{
    public class MessageSendResult
    {
        public INeedSend Message { get; set; }
        public bool IsSuccess { get; set; }
        public string Error { get; set; }
    }
}
