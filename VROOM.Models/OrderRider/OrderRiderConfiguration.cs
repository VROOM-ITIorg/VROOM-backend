using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
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