using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;


namespace vroom.Models
{
    public class Order
    {
        public int Id { get; set; } 
        public string Details { get; set; }
        public OrderState State { get; set; }
        public decimal  OrderPrice { get; set; }
        public decimal DeliveryPrice { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; }
        public float Weight { get; set; }
        public string? Notes { get; set; }
        public bool IsBreakable { get; set; }

        public ICollection<OrderPriority>? Priority { get; set; } 


        public int RiderId { get; set; } //fk to the rider

        public int BusinessId { get; set; } //fk to the Business Owner

        public int PaymentId { get; set; } //fk to the Payment


    }
    public class OrderConfig: IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o=>o.Id);
            builder.Property(o => o.Title).HasMaxLength(100).IsRequired();
            builder.Property(o=>o.Date).IsRequired();
            builder.Property(e => e.Weight).HasColumnType("decimal(18,2)").IsRequired();
            builder.Property(o => o.Notes).HasMaxLength(100);
            builder.Property(o => o.IsBreakable).IsRequired();
            builder.Property(e => e.Details).HasMaxLength(1000);
            //builder.Property(e => e.State).HasDefaultValue(0);
            builder.Property(e => e.OrderPrice).HasColumnType("decimal(18,2)").IsRequired();

            builder.Property(e => e.DeliveryPrice).HasColumnType("decimal(18,2)").IsRequired();

        }
    }
}
