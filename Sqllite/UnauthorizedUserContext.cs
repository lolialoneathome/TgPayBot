using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sqllite
{
    public class UnauthorizedUserContext : DbContext
    {
        protected readonly string _dbPath;
        public UnauthorizedUserContext(string dbPath)
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
