using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class BusinessOwnerConfiguration : IEntityTypeConfiguration<BusinessOwner>
    {

        public void Configure(EntityTypeBuilder<BusinessOwner> modelBuilder)
        {
            modelBuilder
            .HasKey(bo => bo.Id);

            modelBuilder
                .HasOne(bo => bo.User)
                .WithOne(u => u.BusinessOwner)
                .HasForeignKey<BusinessOwner>(bo => bo.UserID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasMany(bo => bo.Riders)
                .WithOne(r => r.BusinessOwner)
                .HasForeignKey(r => r.BusinessID);

            modelBuilder
                .HasMany(bo => bo.RiderAssignments)
                .WithOne(ra => ra.BusinessOwner)
                .HasForeignKey(ra => ra.BusinessID);
        }
    }
}