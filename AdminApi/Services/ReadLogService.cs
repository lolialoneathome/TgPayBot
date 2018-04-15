using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sqllite.Logger;

namespace AdminApi.Services
{
    public class ReadLogService : IReadLogService
    {
        protected readonly LogDbContext _logDbContext;
        public ReadLogService(LogDbContext logDbContext)
        {
            _logDbContext = logDbContext ?? throw new ArgumentNullException(nameof(logDbContext));
        }

        public async Task<IEnumerable<LogMessage>> GetByUserPhone(string phone, int limit, int offset)
        {
            return await _logDbContext.Logs
                .Where(x => x.PhoneNumber == phone)
                .OrderByDescending(x => x.Date)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<IEnumerable<LogMessage>> GetLog(MessageTypes type, int limit, int offset)
        {
            return await _logDbContext.Logs
                    .Where(x => x.Type == type)
                    .OrderByDescending(x => x.Date)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();
        }

        public async Task<int> GetTotal(MessageTypes type)
        {
            return await _logDbContext.Logs.Where(x => x.Type == type).CountAsync();
        }

        public async Task<int> GetTotalByUserPhone(string phone)
        {
            return await _logDbContext.Logs.Where(x => x.PhoneNumber == phone).CountAsync();
        }
    }
}
