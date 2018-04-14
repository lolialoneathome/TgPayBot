using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sqllite.Logger
{
    public class LogDbContextFactory : IDesignTimeDbContextFactory<LogDbContext>
    {
        public LogDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
#if DEBUG
                    .AddJsonFile("appsettings.Development.json", optional: false)
#endif
#if RELEASE
                    .AddJsonFile("appsettings.Production.json", optional: false)
#endif
                    .Build();

            string connectionString = configuration.GetConnectionString("logs");
            Console.WriteLine($"Using connection string: {connectionString}");

            DbContextOptionsBuilder<LogDbContext> builder = new DbContextOptionsBuilder<LogDbContext>();
            builder.UseSqlite(connectionString);
            return new LogDbContext(builder.Options);
        }
    }
}
