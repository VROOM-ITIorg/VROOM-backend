using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vroom.Models
{
    public class OrderPriority
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public string ItemsType { get; set; }

        public string PriorityType { get; set; }
        
    }

    public class OrderPriorityConfig: IEntityTypeConfiguration<OrderPriority>
    {
        public void Configure(EntityTypeBuilder<OrderPriority> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p=>p.ItemsType).HasMaxLength(50).IsRequired();
            builder.HasOne(p => p.Order).WithMany(o=>o.Priority).HasForeignKey(p=>p.OrderId).OnDelete(DeleteBehavior.Cascade);
        }
    }
}
