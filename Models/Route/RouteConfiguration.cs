// RouteConfiguration.cs 
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class RouteConfiguration : IEntityTypeConfiguration<Route>
    {

        public void Configure(EntityTypeBuilder<Route> modelBuilder)
        {
            modelBuilder
            .HasKey(r => r.Id);

            modelBuilder
                .HasOne(r => r.Shipment)
                .WithMany(s => s.Routes)
                .HasForeignKey(r => r.ShipmentID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasMany(r => r.OrderRoutes)
                .WithOne(or => or.Route)
                .HasForeignKey(or => or.RouteID);




        }
    }
}
