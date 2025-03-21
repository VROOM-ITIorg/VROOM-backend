using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class RiderAssignment
    {
        [Key, Column(Order = 0)]
        public int RiderID { get; set; }
        [Key, Column(Order = 1)]
        public int BusinessID { get; set; }
        public DateTime AssignmentDate { get; set; }

        public Rider Rider { get; set; }
        public BusinessOwner BusinessOwner { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class RiderAssignmentConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RiderAssignment>()
                .HasKey(ra => new { ra.RiderID, ra.BusinessID });

            modelBuilder.Entity<RiderAssignment>()
                .HasOne(ra => ra.Rider)
                .WithMany(r => r.RiderAssignments)
                .HasForeignKey(ra => ra.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<RiderAssignment>()
                .HasOne(ra => ra.BusinessOwner)
                .WithMany(bo => bo.RiderAssignments)
                .HasForeignKey(ra => ra.BusinessID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
