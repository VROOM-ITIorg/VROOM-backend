// UserRoleConfiguration.cs 
using Microsoft.EntityFrameworkCore;

namespace VROOM.Models
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
