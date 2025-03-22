using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackID { get; set; }
        public int RiderID { get; set; }
        public int CustomerID { get; set; }
        public int Rating { get; set; }
        public string CustomerName { get; set; }
        public string Message { get; set; }

        public Rider Rider { get; set; }
        public Customer Customer { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class FeedbackConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feedback>()
                .HasKey(f => f.FeedbackID);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Rider)
                .WithMany(r => r.Feedbacks)
                .HasForeignKey(f => f.RiderID).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Customer)
                .WithMany(c => c.FeedbacksProvided)
                .HasForeignKey(f => f.CustomerID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
