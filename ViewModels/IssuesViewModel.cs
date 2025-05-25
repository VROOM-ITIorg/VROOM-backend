using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VROOM.Models;

namespace ViewModels
{
    public class IssuesViewModel
    {
        //public int Id { get; set; }
        public string? RiderID { get; set; }
        public IssueTypeEnum Type { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }
        public DateTime ReportedAt { get; set; } = DateTime.Now;
        public issueSeverityEnum Severity { get; set; }



    }

    public class IssuesWithDetails
    {
        public int Id { get; set; }

        // Remove [Required] from these properties
        public string? RiderID { get; set; }
        public string? RiderName { get; set; }

        public IssueTypeEnum Type { get; set; }

        public DateTime Date { get; set; }
        public string? Note { get; set; }

        public DateTime ReportedAt { get; set; }


        public issueSeverityEnum Severity { get; set; }

        public int? ShipmentID { get; set; }
        public ShipmentStateEnum? ShipmentStatus { get; set; }

        // Remove [Required] from this property
        public string? BusinessOwnerID { get; set; }


        public LocationDto RiderLocation { get; set; }
    }

    public class IssueWithDetails
    {

    }

    public class LocationDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Area { get; set; }
    }

    public class ShipmentDetailsDto
    {
        public DateTime StartTime { get; set; }
        public DateTime? ExpectedEndTime { get; set; }

    }
    public class IssueReportRequest
    {
        [Required]
        [Range(0, 6, ErrorMessage = "Type must be between 0 and 6")]
        public IssueTypeEnum Type { get; set; }

        [Required]
        [Range(0, 2, ErrorMessage = "Severity must be between 0 and 2")]
        public issueSeverityEnum Severity { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [Required]
        public LocationDto RiderLocation { get; set; }
    }

    public class IssueReportResponse
    {
        public int Id { get; set; }
        public string RiderID { get; set; }
        public string RiderName { get; set; }
        public IssueTypeEnum Type { get; set; }
        public issueSeverityEnum Severity { get; set; }
        public string? Note { get; set; }
        public DateTime ReportedAt { get; set; }
        public int? ShipmentID { get; set; }
        public LocationDto RiderLocation { get; set; }
    }
}
