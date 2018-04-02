using Microsoft.Data.Sqlite;
using PayBot.Configuration;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore.Common;
using System;
using System.Collections.Specialized;
using System.Data;

namespace Sender.Quartz
{
    public class QuartzStartup
    {
        private IScheduler _scheduler; // after Start, and until shutdown completes, references the scheduler object
        private IServiceProvider _serviceProvider;


        public QuartzStartup(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        // starts the scheduler, defines the jobs and the triggers
        public void Start()
        {
            if (_scheduler != null)
            {
                throw new InvalidOperationException("Already started.");
            }


            var properties = new NameValueCollection
            {
                ["quartz.serializer.type"] = "json",
            };

            var schedulerFactory = new StdSchedulerFactory(properties);
            _scheduler = schedulerFactory.GetScheduler().Result;

            _scheduler.DeleteJob(new JobKey("SendPaymentsInfo", "group1"));

            _scheduler.Start().Wait();
            _scheduler.JobFactory = new JobFactory(_serviceProvider);
            var paymentsInfoJob = JobBuilder.Create<SendPaymentsInfoJob>()
                .WithIdentity("SendPaymentsInfo", "group1")
                .Build();

            var timeout = ((IConfigService)(_serviceProvider.GetService(typeof(IConfigService)))).Config.SenderTimeout;
            var paymentsInfoTrigger = TriggerBuilder.Create()
                .WithIdentity("SendPaymentsTrigger", "group1")
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(timeout)
                    .RepeatForever())
                .StartNow()
                .Build();

            
            _scheduler.ScheduleJob(paymentsInfoJob, paymentsInfoTrigger).Wait();
        }

        // initiates shutdown of the scheduler, and waits until jobs exit gracefully (within allotted timeout)
        public void Stop()
        {
            if (_scheduler == null)
            {
                return;
            }

            // give running jobs 30 sec (for example) to stop gracefully
            if (_scheduler.Shutdown(waitForJobsToComplete: true).Wait(30000))
            {
                _scheduler = null;
            }
            else
            {
                // jobs didn't exit in timely fashion - log a warning...
            }
        }
    }
}
