using Microsoft.EntityFrameworkCore;
namespace Sqllite
{
    public class UserContext : DbContext
    {
        protected readonly string _dbPath;
        public UserContext(string dbPath)
        {
            _dbPath = dbPath ?? throw new System.Exception("Empty db path");
        }

        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }
}
