using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
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

namespace YourNamespace.Data.Configurations
{
    public class ShipmentConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Shipment>()
                .HasKey(s => s.ShipmentID);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Rider)
                .WithMany(r => r.Shipments)
                .HasForeignKey(s => s.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Route)
                .WithOne(r => r.Shipment)
                .HasForeignKey<Route>(r => r.ShipmentID);
        }
    }
}
