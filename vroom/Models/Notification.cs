using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public DateTime Date { get; set; }

        public User User { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class NotificationConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>()
                .HasKey(n => n.NotificationID);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserID);
        }
    }
}
