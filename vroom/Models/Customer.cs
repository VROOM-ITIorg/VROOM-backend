// Models/Customer.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;

namespace YourNamespace.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public int UserID { get; set; }
        public int OrderID { get; set; } // One-to-one with a specific order (optional)
        public int FeedbackID { get; set; } // One-to-one with feedback

        public User User { get; set; }
        public Order Order { get; set; } // Specific order reference
        public Feedback Feedback { get; set; }
        public ICollection<Feedback> FeedbacksProvided { get; set; }
        public ICollection<Order> Orders { get; set; } // Added: One-to-many with Orders
    }
}

namespace YourNamespace.Data.Configurations
{
    public class CustomerConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>()
                .HasKey(c => c.CustomerID);

            modelBuilder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(c => c.UserID).OnDelete(DeleteBehavior.NoAction);

            //modelBuilder.Entity<Customer>()
            //    .HasOne(c => c.Order)
            //    .WithOne(o => o.Customer)
            //    .HasForeignKey<Customer>(c => c.OrderID);

      

            //modelBuilder.Entity<Customer>()
            //    .HasMany(c => c.Orders)
            //    .WithOne(o => o.Customer)
            //    .HasForeignKey(o => o.CustomerID);
        }
    }
}