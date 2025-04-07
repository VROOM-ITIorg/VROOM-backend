using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class OrderRouteConfiguration : IEntityTypeConfiguration<OrderRoute>
    {

        public void Configure(EntityTypeBuilder<OrderRoute> modelBuilder)
        {
            modelBuilder
    .HasKey(or => new { or.OrderID, or.RouteID });

            modelBuilder
                .HasOne(or => or.Order)
                .WithOne(o => o.OrderRoute)
                .HasForeignKey<OrderRoute>(or => or.OrderID);

            modelBuilder
                .HasOne(or => or.Route)
                .WithMany(r => r.OrderRoutes)
                .HasForeignKey(or => or.RouteID);
        }
    }
}