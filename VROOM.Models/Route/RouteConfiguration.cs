// RouteConfiguration.cs 
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class RouteConfiguration : IEntityTypeConfiguration<Route>
    {
        public static void Configure(ModelBuilder modelBuilder)
        {

        }

        public void Configure(EntityTypeBuilder<Route> modelBuilder)
        {
            modelBuilder
            .HasKey(r => r.RouteID);

            modelBuilder
                .HasOne(r => r.Shipment)
                .WithOne(s => s.Route)
                .HasForeignKey<Route>(r => r.ShipmentID);

            modelBuilder
                .HasMany(r => r.OrderRoutes)
                .WithOne(or => or.Route)
                .HasForeignKey(or => or.RouteID);

        }
    }
}
