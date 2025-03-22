using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        public int CustomerID { get; set; }
        public int RiderID { get; set; }
        public string ItemsType { get; set; }
        public string Title { get; set; }
        public bool IsBreakable { get; set; }
        public string Notes { get; set; }
        public string Details { get; set; }
        public float Weight { get; set; }
        public string Priority { get; set; }
        public string State { get; set; }
        public decimal OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }

        public Customer Customer { get; set; }
        public Rider Rider { get; set; }
        public Payment Payment { get; set; }
        public OrderRoute OrderRoute { get; set; }
        public OrderRider OrderRider { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class OrderConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Rider)
                .WithMany(r => r.OrdersHandled)
                .HasForeignKey(o => o.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderRoute)
                .WithOne(or => or.Order)
                .HasForeignKey<OrderRoute>(or => or.OrderID);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderRider)
                .WithOne(or => or.Order)
                .HasForeignKey<OrderRider>(or => or.OrderID);
        }
    }
}
