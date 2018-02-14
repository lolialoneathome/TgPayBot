using Microsoft.EntityFrameworkCore;

namespace Sqllite
{
    public class StateContext : DbContext
    {
        public StateContext(DbContextOptions options) : base(options) { }
        protected readonly string _dbPath;
        public StateContext(string dbPath)
        {
            _dbPath = dbPath ?? throw new System.Exception("Empty db path");
        }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source=../Db/users.db");
        }

        public DbSet<State> States { get; set; }
    }
}
