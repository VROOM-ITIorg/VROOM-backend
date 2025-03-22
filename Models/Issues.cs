using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;


namespace YourNamespace.Models
{
    public class Issues
    {
        [Key]
        public int IssueID { get; set; }
        public int RiderID { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }

        public Rider Rider { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class IssuesConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Issues>()
                .HasKey(i => i.IssueID);

            modelBuilder.Entity<Issues>()
                .HasOne(i => i.Rider)
                .WithMany(r => r.Issues)
                .HasForeignKey(i => i.RiderID);
        }
    }
}
