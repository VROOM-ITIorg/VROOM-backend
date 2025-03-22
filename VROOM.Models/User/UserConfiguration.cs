// UserConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using VROOM.Models;

namespace VROOM.Models
{
    public class UserConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(u => u.UserID);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey<Address>(a => a.UserID);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserID);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserID);

            modelBuilder.Entity<User>()
                .HasOne(u => u.BusinessOwner)
                .WithOne(bo => bo.User)
                .HasForeignKey<BusinessOwner>(bo => bo.UserID);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Rider)
                .WithOne(r => r.User)
                .HasForeignKey<Rider>(r => r.UserID);

            modelBuilder.Entity<User>()
                .HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserID);
        }
    }
}