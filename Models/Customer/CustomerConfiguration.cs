
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> modelBuilder)
        {
            modelBuilder
                .HasKey(u => u.UserID);
            modelBuilder
              .HasOne(c => c.User)
              .WithOne(u => u.Customer)
              .HasForeignKey<Customer>(c => c.UserID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
