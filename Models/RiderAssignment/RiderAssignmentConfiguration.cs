// RiderAssignmentConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class RiderAssignmentConfiguration : IEntityTypeConfiguration<RiderAssignment>
    {
        public void Configure(EntityTypeBuilder<RiderAssignment> modelBuilder)
        {
            modelBuilder
                .HasKey(ra => new { ra.RiderID, ra.BusinessID });
            modelBuilder
                .HasOne(ra => ra.Rider)
                .WithMany(r => r.RiderAssignments)
                .HasForeignKey(ra => ra.RiderID).OnDelete(DeleteBehavior.NoAction);
            modelBuilder
                .HasOne(ra => ra.BusinessOwner)
                .WithMany(bo => bo.RiderAssignments)
                .HasForeignKey(ra => ra.BusinessID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}