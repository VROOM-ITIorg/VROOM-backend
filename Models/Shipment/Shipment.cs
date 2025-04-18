// Shipment.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Shipment
    {
        public int Id { get; set; }
        public DateTime startTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string  RiderID { get; set; }

        public double BeginningLang { get; set; }
        public double BeginningLat { get; set; }
        public string BeginningArea { get; set; }

        public double EndLang { get; set; }
        public double EndLat { get; set; }
        public string EndArea { get; set; }

        public int MaxConsecutiveDeliveries { get; set; }
        public virtual Rider Rider { get; set; }
        public virtual ICollection<Route> Routes { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; } = DateTime.Now;
    }
}