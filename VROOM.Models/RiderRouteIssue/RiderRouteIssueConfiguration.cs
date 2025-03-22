using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VROOM.Models.RiderRouteIssue
{
    class RiderRouteIssueConfiguration : IEntityTypeConfiguration<RiderRouteIssue>
    {
        public void Configure(EntityTypeBuilder<RiderRouteIssue> builder)
        {
            
        }
    }
}
