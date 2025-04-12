using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VROOM.Models;

namespace VROOM.Data
{
    public class MyDbContext : IdentityDbContext<User>
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        {
        }

        public DbSet<Address> Addresses { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<BusinessOwner> BusinessOwners { get; set; }
        public DbSet<Rider> Riders { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderRider> OrderRiders { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Issues> Issues { get; set; }
        public DbSet<RiderAssignment> RiderAssignments { get; set; }
        public DbSet<OrderRoute> OrderRoutes { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<RiderRouteIssue> RiderRouteIssues { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies()
                          .UseSqlServer("Data Source=.;Initial Catalog=Vroom_DB;Integrated Security=True;TrustServerCertificate=True");
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply configurations (assumed to exist or will be created)
            modelBuilder.ApplyConfiguration(new AddressConfiguration());
            modelBuilder.ApplyConfiguration(new FeedbackConfiguration());
            modelBuilder.ApplyConfiguration(new CustomerConfiguration());
            modelBuilder.ApplyConfiguration(new BusinessOwnerConfiguration());
            modelBuilder.ApplyConfiguration(new RiderConfiguration());
            modelBuilder.ApplyConfiguration(new RouteConfiguration());
            modelBuilder.ApplyConfiguration(new OrderConfiguration());
            modelBuilder.ApplyConfiguration(new OrderRiderConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new IssuesConfiguration());
            modelBuilder.ApplyConfiguration(new RiderAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new OrderRouteConfiguration());
            modelBuilder.ApplyConfiguration(new ShipmentConfiguration());
            modelBuilder.ApplyConfiguration(new RiderRouteIssueConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());

            // Define relationships
            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Rider)
                .WithMany()
                .HasForeignKey(s => s.RiderID);

            modelBuilder.Entity<Shipment>()
                .HasOne(s => s.Route)
                .WithOne(r => r.Shipment)
                .HasForeignKey<Route>(r => r.ShipmentID);

            // Seed test data for Shipments with two records
            modelBuilder.Entity<Shipment>().HasData(
                new Shipment
                {
                    Id = 1,
                    RiderID = 1,
                    Beginning = new DateTime(2025, 4, 10, 8, 0, 0),
                    End = new DateTime(2025, 4, 10, 12, 0, 0),
                    Status = ShipmentStatus.Pending,
                    MaxConsecutiveDeliveries = 5,
                    IsDeleted = false,
                    ModifiedBy = "TestUser",
                    ModifiedAt = new DateTime(2025, 4, 10, 8, 0, 0)
                },
                new Shipment
                {
                    Id = 2,
                    RiderID = 1,
                    Beginning = new DateTime(2025, 4, 11, 9, 0, 0),
                    End = new DateTime(2025, 4, 11, 13, 0, 0),
                    Status = ShipmentStatus.InTransit,
                    MaxConsecutiveDeliveries = 3,
                    IsDeleted = false,
                    ModifiedBy = "TestUser2",
                    ModifiedAt = new DateTime(2025, 4, 11, 9, 0, 0)
                }
            );



            base.OnModelCreating(modelBuilder);
        }
    }
}