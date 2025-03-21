using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;

namespace YourNamespace.Models
{
    public class BusinessOwner
    {
        [Key]
        public int BusinessID { get; set; }
        public int UserID { get; set; }
        public string BankAccount { get; set; }
        public string BusinessType { get; set; }

        public User User { get; set; }
        public ICollection<Rider> Riders { get; set; }
        public ICollection<RiderAssignment> RiderAssignments { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class BusinessOwnerConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BusinessOwner>()
                .HasKey(bo => bo.BusinessID);

            modelBuilder.Entity<BusinessOwner>()
                .HasOne(bo => bo.User)
                .WithOne(u => u.BusinessOwner)
                .HasForeignKey<BusinessOwner>(bo => bo.UserID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BusinessOwner>()
                .HasMany(bo => bo.Riders)
                .WithOne(r => r.BusinessOwner)
                .HasForeignKey(r => r.BusinessID);

            modelBuilder.Entity<BusinessOwner>()
                .HasMany(bo => bo.RiderAssignments)
                .WithOne(ra => ra.BusinessOwner)
                .HasForeignKey(ra => ra.BusinessID);
        }
    }
}
