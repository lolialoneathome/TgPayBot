using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using PayBot.Configuration;
using Sender.DataSource.Base;
using Sender.DataSource.GoogleTabledataSource;
using Sender.DataSource.SenderAgents;
using Sender.Quartz;
using Sender.Services;
using Sqllite;
using Utils;
using Utils.Logger;

namespace Sender
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Warning));

            services.AddDbContext<SqlliteDbContext>(options => options.UseSqlite(Configuration.GetValue<string>("DbPath")));

            services.AddTransient<IPhoneHelper, PhoneHelper>();
            services.AddTransient<ISenderService, SenderService>();
            services.AddTransient<IBotLogger, ToGoogleTableBotLogger>();
            services.AddTransient<IConfigService, FromFileConfigService>(p => new FromFileConfigService(Configuration.GetValue<string>("ConfigPath")));
            services.AddTransient<ISheetsServiceProvider, SheetsServiceProvider>( p => new SheetsServiceProvider(
                p.GetService<IConfigService>(), 
                Configuration.GetSection("Google").GetValue<string>("ClientSecretPath"),
                Configuration.GetSection("Google").GetValue<string>("CredentialsPath")
            ));
            services.AddTransient<ICellService, CellService>();
            services.AddTransient<IDataSource, GoogleTableDataSource>();

            services.AddScoped<ISenderAgentProvider, SenderAgentProvider>();
            services.AddTransient<TelegramSenderAgent>();
            services.AddTransient<TwilioSmsSenderAgent>();

            services.AddTransient<SendPaymentsInfoJob>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure
            (IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime,
            IServiceProvider serviceProvider)
        {
            app.UseMvc();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            //configure NLog
            loggerFactory.AddNLog();
            loggerFactory.ConfigureNLog(Path.Combine(env.ContentRootPath, @"NLog.config"));

            var quartz = new QuartzStartup(serviceProvider);
            lifetime.ApplicationStarted.Register(quartz.Start);
            lifetime.ApplicationStopping.Register(quartz.Stop);
        }
    }
}
