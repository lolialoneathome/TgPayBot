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

        public SendPaymentsInfoJob(ISenderService senderService)
        {
            _senderService = senderService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _senderService.Process(context.CancellationToken);
            var config = new FromFileConfigService("../conf/config.json").Config; //TODO! This shit MUST be changed
            var oldTrigger = await context.Scheduler.GetTrigger(new TriggerKey("SendPaymentsTrigger", "group1"));

            // obtain a builder that would produce the trigger
            var tb = oldTrigger.GetTriggerBuilder();

            var newTrigger =  tb.StartAt(DateTime.Now.AddMinutes(config.SenderTimeout)).WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(config.SenderTimeout)
                    .RepeatForever()).Build();

            await context.Scheduler.RescheduleJob(oldTrigger.Key, newTrigger);
            Console.WriteLine("Trigger fired... changed interval to {0}", config.SenderTimeout);
        }
    }
}