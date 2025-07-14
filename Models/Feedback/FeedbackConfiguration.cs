// VROOM.Models/FeedbackConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            // This static method is unused; remove or implement if needed
        }

        public void Configure(EntityTypeBuilder<Feedback> entity)
        {
            entity.ToTable("Feedbacks");

            entity.HasKey(f => f.Id);

            entity.Property(e => e.RiderID)
                .HasColumnName("RiderID")
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.UserId)
                .HasColumnName("UserId")
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(e => e.Rating)
                .HasColumnName("Rating")
                .IsRequired();

            entity.Property(e => e.Message)
                .HasColumnName("Message")
                .HasMaxLength(500);

            entity.Property(e => e.IsDeleted)
                .HasColumnName("IsDeleted")
                .HasDefaultValue(false);

            entity.Property(e => e.ModifiedBy)
                .HasColumnName("ModifiedBy")
                .HasMaxLength(450);

            entity.Property(e => e.ModifiedAt)
                .HasColumnName("ModifiedAt") // Explicitly map to ModifiedAt
                .HasDefaultValueSql("GETDATE()"); // Set default to current timestamp

            entity.HasOne(f => f.Rider)
                .WithMany(r => r.Feedbacks)
                .HasForeignKey(f => f.RiderID)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(f => f.Customer)
                .WithMany(c => c.FeedbacksProvided)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}