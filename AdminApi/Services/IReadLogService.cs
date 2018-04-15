using Sqllite.Logger;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AdminApi.Services
{
    public interface IReadLogService
    {
        Task<IEnumerable<LogMessage>> GetLog(MessageTypes type, int limit, int offset);
        Task<int> GetTotal(MessageTypes type);
        Task<IEnumerable<LogMessage>> GetByUserPhone(string phone, int limit, int offset);
        Task<int> GetTotalByUserPhone(string phone);
    }
}
