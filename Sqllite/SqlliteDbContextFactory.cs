using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;

namespace Sqllite
{
    public class SqlliteDbContextFactory : IDesignTimeDbContextFactory<SqlliteDbContext>
    {
        public SqlliteDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
#if DEBUG
                    .AddJsonFile("appsettings.Development.json", optional: false)
#endif
#if RELEASE
                    .AddJsonFile("appsettings.Production.json", optional: false)
#endif
                    .Build();

            string connectionString = configuration.GetConnectionString("Sqllite");
            Console.WriteLine($"Using connection string: {connectionString}");

            DbContextOptionsBuilder<SqlliteDbContext> builder = new DbContextOptionsBuilder<SqlliteDbContext>();
            builder.UseSqlite(connectionString);
            return new SqlliteDbContext(builder.Options);
        }
    }
}
