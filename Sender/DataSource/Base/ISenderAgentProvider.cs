namespace Sender.DataSource.Base
{
    public interface ISenderAgentProvider
    {
        ISenderAgent Resolve(SenderType type);
    }
}
