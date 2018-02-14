using Microsoft.Data.Sqlite;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.AdoJobStore.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Sender.Quartz
{
    public class QuartzStartup
    {
        private IScheduler _scheduler; // after Start, and until shutdown completes, references the scheduler object

        // starts the scheduler, defines the jobs and the triggers
        public void Start()
        {
            if (_scheduler != null)
            {
                throw new InvalidOperationException("Already started.");
            }

            DbProvider.RegisterDbMetadata("sqlite-provider", new DbMetadata()
            {
                AssemblyName = typeof(SqliteConnection).Assembly.GetName().Name,
                ConnectionType = typeof(SqliteConnection),
                CommandType = typeof(SqliteCommand),
                ParameterType = typeof(SqliteParameter),
                ParameterDbType = typeof(DbType),
                ParameterDbTypePropertyName = "DbType",
                ParameterNamePrefix = "@",
                ExceptionType = typeof(SqliteException),
                BindByName = true
            });


            var properties = new NameValueCollection
            {
                // json serialization is the one supported under .NET Core (binary isn't)
                ["quartz.serializer.type"] = "json",

                // the following setup of job store is just for example and it didn't change from v2
                ["quartz.jobStore.type"] = "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz",
                ["quartz.jobStore.useProperties"] = "false",
                ["quartz.jobStore.dataSource"] = "default",
                ["quartz.jobStore.tablePrefix"] = "QRTZ_",
                ["quartz.jobStore.driverDelegateType"] = "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz",
                ["quartz.dataSource.default.provider"] = "sqlite-provider",
                ["quartz.dataSource.default.connectionString"] = @"DataSource=quartz.db"
            };

            var schedulerFactory = new StdSchedulerFactory(properties);
            _scheduler = schedulerFactory.GetScheduler().Result;
            _scheduler.Start().Wait();

            var paymentsInfoJob = JobBuilder.Create<SendPaymentsInfoJob>()
                .WithIdentity("SendPaymentsInfo")
                .Build();
            var paymentsInfoTrigger = TriggerBuilder.Create()
                .WithIdentity("UserEmailsCron")
                .StartNow()
                .WithCronSchedule("0 0 / 5 * ** ?")
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
