using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class UserRole
    {
        [Key, Column(Order = 0)]
        public int UserID { get; set; }
        [Key, Column(Order = 1)]
        public string Role { get; set; }

        public User User { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class UserRoleConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserID, ur.Role });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
