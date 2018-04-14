using Microsoft.EntityFrameworkCore;

namespace Sqllite
{
    public class SqlliteDbContext : DbContext
    {
        public SqlliteDbContext(DbContextOptions<SqlliteDbContext> options)
        : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<UnauthorizedUser> UnauthorizedUsers { get; set; }
        public DbSet<State> States { get; set; }
    }
}
