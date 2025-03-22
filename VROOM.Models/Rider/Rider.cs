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

        public BusinessOwner BusinessOwner { get; set; }
        public User User { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; }
        public ICollection<Shipment> Shipments { get; set; }
        public ICollection<Issues> Issues { get; set; }
        public ICollection<RiderAssignment> RiderAssignments { get; set; }
        public ICollection<Order> OrdersHandled { get; set; }
        public ICollection<OrderRider> OrderRiders { get; set; }
    }
}