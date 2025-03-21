using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class OrderRider
    {
        [Key]
        public int OrderRiderID { get; set; }
        public int OrderID { get; set; }
        public int RiderID { get; set; }
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public bool IsBreakable { get; set; }
        public string Notes { get; set; }
        public float Weight { get; set; }
        public string Priority { get; set; }
        public string State { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Vehicle { get; set; }
        public string Location { get; set; }
        public string ExperienceLevel { get; set; }
        public float Rating { get; set; }

        public Order Order { get; set; }
        public Rider Rider { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class OrderRiderConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderRider>()
                .HasKey(or => or.OrderRiderID);

            modelBuilder.Entity<OrderRider>()
                .HasOne(or => or.Order)
                .WithOne(o => o.OrderRider)
                .HasForeignKey<OrderRider>(or => or.OrderID);

            modelBuilder.Entity<OrderRider>()
                .HasOne(or => or.Rider)
                .WithMany(r => r.OrderRiders)
                .HasForeignKey(or => or.RiderID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
