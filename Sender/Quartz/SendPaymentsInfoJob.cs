using System.Threading.Tasks;
using Quartz;
using Sender.Services;

namespace Sender.Quartz
{
    internal class SendPaymentsInfoJob : IJob
    {
        protected readonly ISenderService _senderService;
        public SendPaymentsInfoJob(ISenderService senderService)
        {
            _senderService = senderService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return _senderService.Send();
        }
    }
}