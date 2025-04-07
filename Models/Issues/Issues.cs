using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{

    public class Issues
    {
        [Key]
        public int Id { get; set; }
        public string  RiderID { get; set;}
        public IssueTypeEnum Type { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public issueSeverityEnum Severity { get; set; }



        public virtual Rider Rider { get; set; }
        public virtual RiderRouteIssue RiderRouteIssue { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime ModifiedAt { get; set; } = DateTime.Now;
    }


}