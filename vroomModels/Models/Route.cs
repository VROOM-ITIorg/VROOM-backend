using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class Route
    {
        [Key]
        public int RouteID { get; set; }
        public int ShipmentID { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Waypoints { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public float SafetyIndex { get; set; }
        public DateTime DateTime { get; set; }

        public Shipment Shipment { get; set; }
        public ICollection<OrderRoute> OrderRoutes { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
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
