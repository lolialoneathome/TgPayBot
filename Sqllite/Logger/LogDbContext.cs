using Microsoft.EntityFrameworkCore;

namespace Sqllite.Logger
{
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options)
            : base(options)
        {

        }

        public DbSet<LogMessage> Logs { get; set; }
    }
}
