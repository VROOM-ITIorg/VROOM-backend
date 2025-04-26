// Route.cs 
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Route
    {
        public int Id { get; set; }
        public int? ShipmentID { get; set; }
        
        public double OriginLang { get; set; }
        public double OriginLat { get; set; }
        public string OriginArea { get; set; }
        public double DestinationLang { get; set; }
        public double DestinationLat { get; set; }
        public string DestinationArea { get; set; }
        public string? Waypoints { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public float SafetyIndex { get; set; }
        public DateTime dateTime { get; set; }

        [JsonIgnore]
        public virtual Shipment Shipment { get; set; }
        public virtual ICollection<OrderRoute> OrderRoutes { get; set; }
        public virtual ICollection<RiderRouteIssue> RiderRouteIssues { get; set; }

        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}