using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class IssuesConfiguration : IEntityTypeConfiguration<Issues>
    {

        public void Configure(EntityTypeBuilder<Issues> modelBuilder)
        {
            modelBuilder
            .HasKey(i => i.Id);

            modelBuilder
                .HasOne(i => i.Rider)
                .WithMany(r => r.Issues)
                .HasForeignKey(i => i.RiderID);
        }
    }
}