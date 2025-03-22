// RiderAssignmentConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
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