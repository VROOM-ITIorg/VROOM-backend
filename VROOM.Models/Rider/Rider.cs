using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class Rider
    {
        [Key]
        public int RiderID { get; set; }
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Vehicle { get; set; }
        public string Location { get; set; }
        public string ExperienceLevel { get; set; }
        public float Rating { get; set; }

        public virtual BusinessOwner BusinessOwner { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Feedback> Feedbacks { get; set; }
        public virtual ICollection<Shipment> Shipments { get; set; }
        public virtual ICollection<Issues> Issues { get; set; }
        public virtual ICollection<RiderAssignment> RiderAssignments { get; set; }
        public virtual ICollection<Order> OrdersHandled { get; set; }
        public virtual ICollection<OrderRider> OrderRiders { get; set; }
        public virtual ICollection<RiderRouteIssue> RiderRouteIssues { get; set; }
    }
}