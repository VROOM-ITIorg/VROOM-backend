// RouteConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class RouteConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Route>()
                .HasKey(r => r.RouteID);

            modelBuilder.Entity<Route>()
                .HasOne(r => r.Shipment)
                .WithOne(s => s.Route)
                .HasForeignKey<Route>(r => r.ShipmentID);

            modelBuilder.Entity<Route>()
                .HasMany(r => r.OrderRoutes)
                .WithOne(or => or.Route)
                .HasForeignKey(or => or.RouteID);
        }
    }
}
