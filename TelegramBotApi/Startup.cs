﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayBot.Configuration;
using Sqllite;
using Telegram.Bot;
using TelegramBotApi.Services;
using Utils;
using Utils.Logger;

namespace TelegramBotApi
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
            services.AddScoped<ITelegramBotClient, TelegramBotClient>();
            services.AddDbContext<UserContext>(options => options.UseSqlite("Data Source=../Db/users.db"));
            services.AddDbContext<StateContext>(options => options.UseSqlite("Data Source=../Db/users.db"));

            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));


            services.AddScoped<IPhoneHelper, PhoneHelper>();
            services.AddScoped<IBotLogger, ToGoogleTableBotLogger>();
            services.AddScoped<ISheetsServiceProvider, SheetsServiceProvider>(p => new SheetsServiceProvider(
                p.GetService<IConfigService>(),
                Configuration.GetSection("Google").GetValue<string>("ClientSecretPath"),
                Configuration.GetSection("Google").GetValue<string>("CredentialsPath"))
            );

            services.AddScoped<IConfigService, FromFileConfigService>( p => new FromFileConfigService("../conf/config.json"));
            services.AddScoped<IUserMessageService, UserMessageService>();
            services.AddScoped<IAdminMessageService, AdminMessageService>();
            services.AddScoped<IPhoneNumberVerifier, FakePhoneNumberVerifier>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            lifetime.ApplicationStarted.Register(setWebhook);
            lifetime.ApplicationStopping.Register(deleteWebHook);
            

        }

        private void deleteWebHook()
        {
            Bot.Api.DeleteWebhookAsync().Wait();
        }

        private void setWebhook()
        {
            Bot.Api.SetWebhookAsync("https://a2890bc2.ngrok.io/webhook").Wait();
        }
    }
}