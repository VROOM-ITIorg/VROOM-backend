using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;

namespace ViewModels
{
    public class IssuesViewModel
    {
        //public int Id { get; set; }
        public string RiderID { get; set; }
        public IssueTypeEnum Type { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public issueSeverityEnum Severity { get; set; }



    }
}
