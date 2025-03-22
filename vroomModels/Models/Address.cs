using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;

namespace YourNamespace.Models
{
    public class Address
    {
        [Key, ForeignKey("User")]
        public int UserID { get; set; }
        public string Lang { get; set; }
        public float Lat { get; set; }
        public string Area { get; set; }

        public User User { get; set; }
    }
}

namespace YourNamespace.Data.Configurations
{
    public class AddressConfiguration
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>()
                .HasKey(a => a.UserID);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.User)
                .WithOne(u => u.Address)
                .HasForeignKey<Address>(a => a.UserID).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
