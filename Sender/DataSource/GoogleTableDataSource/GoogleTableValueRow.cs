using Sender.DataSource;
using Sender.DataSource.Base;
using System;

namespace Sender.DataSource.GoogleTabledataSource
{
    public class GoogleTableValueRow : IMessage
    {
        /// <summary>
        /// Fields with data from DataSource
        /// </summary>
        public DateTime LastModifiedDate { get; set; }
        public string Status { get; set; }
        public string IsMessageAlreadySended { get; set; }
        public string Text { get; set; }
        public string To { get; set; }

        /// <summary>
        /// Fields with data for update this message
        /// </summary>
        public string Table { get; set;}
        public string CellForUpdate { get; set; }
    }
}