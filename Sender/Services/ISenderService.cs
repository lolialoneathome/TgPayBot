using System.Threading;
using System.Threading.Tasks;

namespace Sender.Services
{
    public interface ISenderService
    {
        Task Process(CancellationToken cancellation);
    }
}
