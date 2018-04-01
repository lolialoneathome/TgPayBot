using System;
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

        public async Task Execute(IJobExecutionContext context)
        {
            await _senderService.Process(context.CancellationToken);
        }
    }
}