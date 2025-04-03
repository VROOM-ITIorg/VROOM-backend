using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
 

        public void Configure(EntityTypeBuilder<Order> modelBuilder)
        {
            modelBuilder
                .HasOne(o => o.Customer)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.CustomerID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne(o => o.Rider)
                .WithMany(r => r.OrdersHandled)
                .HasForeignKey(o => o.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne(o => o.Payment)
                .WithOne(p => p.Order)
                .HasForeignKey<Payment>(p => p.OrderID);

            modelBuilder
                .HasOne(o => o.OrderRoute)
                .WithOne(or => or.Order)
                .HasForeignKey<OrderRoute>(or => or.OrderID);

            modelBuilder
                .HasOne(o => o.OrderRider)
                .WithOne(or => or.Order)
                .HasForeignKey<OrderRider>(or => or.OrderID);

            modelBuilder
                .Property(o => o.State)
                .HasDefaultValue(OrderState.Pending);
        }
    }
}