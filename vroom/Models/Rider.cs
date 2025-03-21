using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class Rider
    {
        [Key]
        public int RiderID { get; set; }
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Vehicle { get; set; }
        public string Location { get; set; }
        public string ExperienceLevel { get; set; }
        public float Rating { get; set; }

        public BusinessOwner BusinessOwner { get; set; }
        public User User { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; }
        public ICollection<Shipment> Shipments { get; set; }
        public ICollection<Issues> Issues { get; set; }
        public ICollection<RiderAssignment> RiderAssignments { get; set; }
        public ICollection<Order> OrdersHandled { get; set; }
        public ICollection<OrderRider> OrderRiders { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
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
