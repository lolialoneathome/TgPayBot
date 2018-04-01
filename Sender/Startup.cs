using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using PayBot.Configuration;
using Sender.DataSource.Base;
using Sender.DataSource.GoogleTabledataSource;
using Sender.Quartz;
using Sender.Services;
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

            services.AddScoped<IPhoneHelper, PhoneHelper>();
            services.AddScoped<ISenderService, SenderService>();
            services.AddScoped<IBotLogger, ToGoogleTableBotLogger>();
            services.AddScoped<ISheetsServiceProvider, SheetsServiceProvider>( p => new SheetsServiceProvider(
                p.GetService<Config>(), 
                Configuration.GetSection("Google").GetValue<string>("ClientSecretPath"),
                Configuration.GetSection("Google").GetValue<string>("CredentialsPath")
            ));
            services.AddScoped<ICellService, CellService>();
            services.AddScoped<IMessageDataSource, GoogleTableDataSource>();
            services.AddScoped<SendPaymentsInfoJob>();
            services.AddSingleton(p =>
            {
                var json = System.IO.File.ReadAllText(Configuration.GetSection("ConfigPath").Get<string>());
                return JsonConvert.DeserializeObject<Config>(json);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure
            (IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime,
            IServiceProvider serviceProvider)
        {
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
