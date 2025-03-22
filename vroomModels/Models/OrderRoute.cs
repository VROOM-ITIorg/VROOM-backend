using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class OrderRoute
    {
        [Key, Column(Order = 0)]
        public int OrderID { get; set; }
        [Key, Column(Order = 1)]
        public int RouteID { get; set; }
        public string Status { get; set; }

        public Order Order { get; set; }
        public Route Route { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
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
