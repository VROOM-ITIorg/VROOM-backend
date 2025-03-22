using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
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