using System.Threading.Tasks;

namespace Sender.DataSource.Base
{
    public interface ISenderAgent
    {
        Task<MessageSendResult> Send(INeedSend message);
    }
}
