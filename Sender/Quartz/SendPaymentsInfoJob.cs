using System;
using System.Threading.Tasks;
using PayBot.Configuration;
using Quartz;
using Sender.Services;

namespace Sender.Quartz
{
    internal class SendPaymentsInfoJob : IJob
    {
        protected readonly ISenderService _senderService;
        protected readonly IConfigService _configService;
        public SendPaymentsInfoJob(ISenderService senderService, IConfigService configService)
        {
            _senderService = senderService ?? throw new ArgumentNullException(nameof(senderService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _senderService.Process(context.CancellationToken);
            var config = _configService.Config;
            var oldTrigger = await context.Scheduler.GetTrigger(new TriggerKey("SendPaymentsTrigger", "group1"));

            // obtain a builder that would produce the trigger
            var tb = oldTrigger.GetTriggerBuilder();

            var newTrigger =  tb.StartAt(DateTime.Now.AddMinutes(config.SenderTimeout)).WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(config.SenderTimeout)
                    .RepeatForever()).Build();

            await context.Scheduler.RescheduleJob(oldTrigger.Key, newTrigger);
        }
    }
}