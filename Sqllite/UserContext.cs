using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
namespace Sqllite
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
        : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<UnauthorizedUser> UnauthorizedUsers { get; set; }
    }
}
