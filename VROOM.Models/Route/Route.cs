// Route.cs 
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Route
    {
        public int Id { get; set; }
        public int ShipmentID { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Waypoints { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public float SafetyIndex { get; set; }
        public DateTime DateTime { get; set; }

        public virtual Shipment Shipment { get; set; }
        public virtual ICollection<OrderRoute> OrderRoutes { get; set; }
        public virtual ICollection<RiderRouteIssue> RiderRouteIssues { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}