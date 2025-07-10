using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models.JobRecords
{
    public class JobRecord
    {
        public int Id { get; set; }
        public string JobId { get; set; }
        public int ShipmentId { get; set; } 
        public string Status { get; set; } 
        public DateTime ScheduledAt { get; set; }
        public DateTime? CompletedAt { get; set; } 
        public string HangfireJobId { get; set; } 
        public string? ErrorMessage { get; set; } 
    }
}
