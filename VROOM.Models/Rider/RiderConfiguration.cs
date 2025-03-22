// RiderConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class RiderConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rider>()
                .HasKey(r => r.RiderID);

            modelBuilder.Entity<Rider>()
                .HasOne(r => r.BusinessOwner)
                .WithMany(bo => bo.Riders)
                .HasForeignKey(r => r.BusinessID);

            modelBuilder.Entity<Rider>()
                .HasOne(r => r.User)
                .WithOne(u => u.Rider)
                .HasForeignKey<Rider>(r => r.UserID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Rider>()
                .HasMany(r => r.Feedbacks)
                .WithOne(f => f.Rider)
                .HasForeignKey(f => f.RiderID);

            modelBuilder.Entity<Rider>()
                .HasMany(r => r.Shipments)
                .WithOne(s => s.Rider)
                .HasForeignKey(s => s.RiderID);

            modelBuilder.Entity<Rider>()
                .HasMany(r => r.Issues)
                .WithOne(i => i.Rider)
                .HasForeignKey(i => i.RiderID);

            modelBuilder.Entity<Rider>()
                .HasMany(r => r.RiderAssignments)
                .WithOne(ra => ra.Rider)
                .HasForeignKey(ra => ra.RiderID);

            modelBuilder.Entity<Rider>()
                .HasMany(r => r.OrdersHandled)
                .WithOne(o => o.Rider)
                .HasForeignKey(o => o.RiderID);

            modelBuilder.Entity<Rider>()
                .HasMany(r => r.OrderRiders)
                .WithOne(or => or.Rider)
                .HasForeignKey(or => or.RiderID);
        }
    }
}