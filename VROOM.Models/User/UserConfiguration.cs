// UserConfiguration.cs 
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VROOM.Models;

namespace VROOM.Models
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> modelBuilder)
        {

            modelBuilder
                .HasOne(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey<Address>(a => a.UserID);

            modelBuilder
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserID);

            modelBuilder
                .HasOne(u => u.Customer)
                .WithOne(c => c.User)
                .HasForeignKey<Customer>(c => c.UserID);

            modelBuilder
                .HasOne(u => u.BusinessOwner)
                .WithOne(bo => bo.User)
                .HasForeignKey<BusinessOwner>(bo => bo.UserID);

            modelBuilder
                .HasOne(u => u.Rider)
                .WithOne(r => r.User)
                .HasForeignKey<Rider>(r => r.UserID);

        }
    }
}