// RiderConfiguration.cs 
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class RiderConfiguration : IEntityTypeConfiguration<Rider>
    {

        public void Configure(EntityTypeBuilder<Rider> modelBuilder)
        {
            modelBuilder
            .HasKey(r => r.Id);

            modelBuilder
                .HasOne(r => r.BusinessOwner)
                .WithMany(bo => bo.Riders)
            .HasForeignKey(r => r.BusinessID);

            modelBuilder
                .HasOne(r => r.User)
                .WithOne(u => u.Rider)
                .HasForeignKey<Rider>(r => r.UserID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasMany(r => r.Feedbacks)
                .WithOne(f => f.Rider)
                .HasForeignKey(f => f.RiderID);

            modelBuilder
                .HasMany(r => r.Shipments)
                .WithOne(s => s.Rider)
                .HasForeignKey(s => s.RiderID);

            modelBuilder
                .HasMany(r => r.Issues)
                .WithOne(i => i.Rider)
                .HasForeignKey(i => i.RiderID);

            modelBuilder
                .HasMany(r => r.RiderAssignments)
                .WithOne(ra => ra.Rider)
                .HasForeignKey(ra => ra.RiderID);

            modelBuilder
                .HasMany(r => r.OrdersHandled)
                .WithOne(o => o.Rider)
                .HasForeignKey(o => o.RiderID);

            modelBuilder
                .HasMany(r => r.OrderRiders)
                .WithOne(or => or.Rider)
                .HasForeignKey(or => or.RiderID);
        }
    }
}