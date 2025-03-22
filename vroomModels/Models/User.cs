// Models/User.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;

namespace YourNamespace.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }

        public Address Address { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public Customer Customer { get; set; }
        public BusinessOwner BusinessOwner { get; set; }
        public Rider Rider { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
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