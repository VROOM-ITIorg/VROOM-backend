// Shipment.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Shipment
    {
        public int Id{ get; set; }
        public int RiderID { get; set; }
        public DateTime Beginning { get; set; }
        public DateTime End { get; set; }
        public ShipmentStatus Status { get; set; }
        public int MaxConsecutiveDeliveries { get; set; }
        public virtual Rider Rider { get; set; }
        public virtual Route Route { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }

    public enum ShipmentStatus
    {
        Pending,
        InTransit,
        Delivered,
        Cancelled,
        Delayed
    }
}