using Sqllite.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminApi.DTOs
{
    public class LogMessagesListDto
    {
        public int Total { get; set; }
        public IEnumerable<LogMessage> List { get; set; }
    }
}
