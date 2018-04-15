using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdminApi.DTOs
{
    public class LogMessageDto
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
        public string Text { get; set; }
        public string PhoneNumber { get; set; }
    }
}
