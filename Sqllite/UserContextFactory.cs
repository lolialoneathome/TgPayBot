using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using PayBot.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sqllite
{
    public class UserContextFactory : IDesignTimeDbContextFactory<UserContext>
    {
        protected readonly Config _config;
        private const string _configPath = "../conf/config.json";
        public UserContextFactory() {
            var json = System.IO.File.ReadAllText(_configPath);
            _config =  JsonConvert.DeserializeObject<Config>(json);
        }

        public UserContext Create(DbContextFactoryOptions options)
        {
            var builder = new DbContextOptionsBuilder<UserContext>();


            builder.UseSqlite(_config.DbPath);

            return new UserContext(builder.Options);
        }

        public UserContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<UserContext>();

            builder.UseSqlite(_config.DbPath);

            return new UserContext(builder.Options);
        }
    }
}
