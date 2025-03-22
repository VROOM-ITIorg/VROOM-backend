using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
    {
        public static void Configure(ModelBuilder modelBuilder)
        {

        }

        public void Configure(EntityTypeBuilder<Feedback> modelBuilder)
        {
            //modelBuilder.Entity<Feedback>()
            //    .HasKey(f => f.Id);

            modelBuilder
                .HasOne(f => f.Rider)
                .WithMany(r => r.Feedbacks)
                .HasForeignKey(f => f.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne(f => f.Customer)
                .WithMany(c => c.FeedbacksProvided)
                .HasForeignKey(f => f.CustomerID).OnDelete(DeleteBehavior.NoAction);
            throw new NotImplementedException();
        }
    }
}
