using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class OrderRouteConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderRoute>()
                .HasKey(or => new { or.OrderID, or.RouteID });

            modelBuilder.Entity<OrderRoute>()
                .HasOne(or => or.Order)
                .WithOne(o => o.OrderRoute)
                .HasForeignKey<OrderRoute>(or => or.OrderID);

            modelBuilder.Entity<OrderRoute>()
                .HasOne(or => or.Route)
                .WithMany(r => r.OrderRoutes)
                .HasForeignKey(or => or.RouteID);
        }
    }
}