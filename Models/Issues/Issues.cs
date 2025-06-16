using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{

    public class Issues
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string RiderID { get; set; }

        [ForeignKey("Shipment")]
        public int? ShipmentID { get; set; }

        [Required]
        public IssueTypeEnum Type { get; set; }

        [Required]
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Area { get; set; }
        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [StringLength(1000)]
        public string? Note { get; set; }

        [Required]
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public issueSeverityEnum Severity { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;

        [StringLength(450)]
        public string? ModifiedBy { get; set; }

        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RiderID")]
        public virtual Rider Rider { get; set; }

        [ForeignKey("ShipmentID")]
        public virtual Shipment Shipment { get; set; }

        public virtual RiderRouteIssue RiderRouteIssue { get; set; }
    }


}