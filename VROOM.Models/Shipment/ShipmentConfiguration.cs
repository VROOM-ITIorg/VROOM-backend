// ShipmentConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
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
