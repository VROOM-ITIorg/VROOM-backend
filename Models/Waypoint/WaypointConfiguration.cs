using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public class WaypointConfiguration : IEntityTypeConfiguration<Waypoint>
    {
        public void Configure(EntityTypeBuilder<Waypoint> modelBuilder)
        {
            modelBuilder
                .HasOne(w => w.Shipment)
                .WithMany(s => s.waypoints)
                .HasForeignKey(w => w.ShipmentID).OnDelete(DeleteBehavior.NoAction);

        }
    }
}
