using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VROOM.Models
{
    public class RiderRouteIssueConfiguration : IEntityTypeConfiguration<RiderRouteIssue>
    {
        public void Configure(EntityTypeBuilder<RiderRouteIssue> modelBuilder)
        {
            modelBuilder
                .HasKey(rri => rri.Id);

            modelBuilder
                            .HasKey(rri => rri.Id);

            modelBuilder
                .HasOne(rri => rri.Rider)
                .WithMany(r => r.RiderRouteIssues)
                .HasForeignKey(rri => rri.RiderID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne(rri => rri.Route)
                .WithMany(r => r.RiderRouteIssues)
                .HasForeignKey(rri => rri.RouteID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder
                .HasOne(rri => rri.Issue)
                .WithOne(i => i.RiderRouteIssue)
                .HasForeignKey<RiderRouteIssue>(rri => rri.IssueID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
