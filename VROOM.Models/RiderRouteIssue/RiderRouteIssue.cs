using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public class RiderRouteIssue
    {
        public int Id { get; set; }
        public int RiderID { get; set; }
        public int RouteID { get; set; }
        public int IssueID { get; set; }
        public string Description { get; set; }
        public DateTime ReportedAt { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }

        public virtual Rider Rider { get; set; }
        public virtual Route Route { get; set; }
        public virtual Issues Issue { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}
