using PayBot.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sender.DataSource.Base
{
    /// <summary>
    /// Implement object for work with Messages data Source
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Get List of all messages in DS
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<INeedSend>> GetMessages(Config config);

        /// <summary>
        /// Batch update messages status. All messages must be from ONE table
        /// </summary>
        /// <param name="messages"></param>
        /// <returns></returns>
        Task<bool> UpdateMessageStatus(IEnumerable<INeedSend> messages);
    }
}
