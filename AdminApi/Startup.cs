using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayBot.Configuration;
using Sqllite;
using Sqllite.Logger;
using NLog.Extensions.Logging;
using AdminApi.Services;
using Utils;
using AdminApi.DTOs;

namespace AdminApi
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

            services.AddScoped<IConfigService, FromFileConfigService>(p => new FromFileConfigService(Configuration.GetValue<string>("ConfigPath")));
            services.AddDbContext<SqlliteDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("users")));
            services.AddDbContext<LogDbContext>(options => options.UseSqlite(Configuration.GetConnectionString("logs")));

            services.AddScoped<IReadLogService, ReadLogService>();
            services.AddTransient<IPhoneHelper, PhoneHelper>();
            services.AddCors();
            // services.AddAutoMapper();
            services.AddSingleton(provider => new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingProfile(provider.GetService<IPhoneHelper>()));
            }).CreateMapper());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            //configure NLog
            loggerFactory.AddNLog();
            loggerFactory.ConfigureNLog(Path.Combine(env.ContentRootPath, @"NLog.config"));

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            app.UseMvc();
        }
    }
}
