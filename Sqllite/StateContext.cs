using Microsoft.EntityFrameworkCore;

namespace Sqllite
{
    public class StateContext : DbContext
    {
        public StateContext(DbContextOptions<StateContext> options)
                : base(options)
        {

        }

        public DbSet<State> States { get; set; }
    }
}
