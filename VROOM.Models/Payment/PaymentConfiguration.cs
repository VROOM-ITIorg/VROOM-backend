using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {

        public void Configure(EntityTypeBuilder<Payment> modelBuilder)
        {
            modelBuilder
            .HasKey(p => p.PaymentID);

            modelBuilder
                .HasOne(p => p.Order)
                .WithOne(o => o.Payment)
                .HasForeignKey<Payment>(p => p.OrderID);
        }
    }
}