// Shipment.cs

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using VROOM.Models;


namespace VROOM.Models
{
    public class Shipment
    {
        [Key]
        public int ShipmentID { get; set; }
        [Key, ForeignKey("Rider")]
        public int RiderID { get; set; }
        public string Beginning { get; set; }
        public string End { get; set; }
        public int MaxConsecutiveDeliveries { get; set; }

        public Rider Rider { get; set; }
        public Route Route { get; set; }
    }
}