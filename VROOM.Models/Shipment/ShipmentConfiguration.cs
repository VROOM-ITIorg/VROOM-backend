// ShipmentConfiguration.cs 
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
    {

        public void Configure(EntityTypeBuilder<Shipment> modelBuilder)
        {
            modelBuilder
                .HasKey(s => s.ShipmentID);

            modelBuilder
                .HasOne(s => s.Rider)
                .WithMany(r => r.Shipments)
                .HasForeignKey(s => s.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne(s => s.Route)
                .WithOne(r => r.Shipment)
                .HasForeignKey<Route>(r => r.ShipmentID);
        }
    }
}
