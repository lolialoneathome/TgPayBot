using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using PayBot.Configuration;
using System;
using TelegramListener.Core;
using Utils;
using Utils.Logger;

namespace TelegramListener
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        private const string _configPath = "../conf/config.json";
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddSingleton<ILoggerFactory, LoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            services.AddLogging((builder) => builder.SetMinimumLevel(LogLevel.Warning));


            services.AddScoped<IPhoneHelper, PhoneHelper>();
            services.AddScoped<IBotLogger, ToGoogleTableBotLogger>();
            services.AddScoped<ISheetsServiceProvider, SheetsServiceProvider>();

            services.AddScoped<EventsTelegramBotClient>();

            
            services.AddSingleton(p =>
            {
                var json = System.IO.File.ReadAllText(_configPath);
                return JsonConvert.DeserializeObject<Config>(json);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IHostingEnvironment env, 
            IServiceProvider serviceProvider,
            IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            //configure NLog
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            loggerFactory.ConfigureNLog("nlog.config");


            var listener = (EventsTelegramBotClient)(serviceProvider.GetService(typeof(EventsTelegramBotClient)));
            lifetime.ApplicationStarted.Register(listener.Start);
            lifetime.ApplicationStopping.Register(listener.Stop);

        }
    }
}
