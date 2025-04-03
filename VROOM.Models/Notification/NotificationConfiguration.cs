// NotificationConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {


        public void Configure(EntityTypeBuilder<Notification> modelBuilder) =>
            //modelBuilder.Entity<Notification>()
            //    .HasKey(n => n.Id);

            modelBuilder
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserID);
    }
}