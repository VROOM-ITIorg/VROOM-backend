using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class OrderRiderConfiguration : IEntityTypeConfiguration<OrderRider>
    {
        public void Configure(EntityTypeBuilder<OrderRider> modelBuilder)
        {
            modelBuilder
                .HasKey(or => or.Id);

            modelBuilder
                .HasOne(or => or.Order)
                .WithOne(o => o.OrderRider)
                .HasForeignKey<OrderRider>(or => or.OrderID);

            modelBuilder
                .HasOne(or => or.Rider)
                .WithMany(r => r.OrderRiders)
                .HasForeignKey(or => or.RiderID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}