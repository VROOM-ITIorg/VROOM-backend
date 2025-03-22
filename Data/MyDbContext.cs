using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using YourNamespace.Data.Configurations;
using YourNamespace.Models;

namespace YourNamespace.Data
{
    public class MyDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public MyDbContext(DbContextOptions<MyDbContext> options, IConfiguration configuration) 
            : base(options)
        {
            _configuration = configuration;
        }

        public MyDbContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=VROOM;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Integrated Security = true");
            }
        }

        public DbSet<User> Users { get; set; }
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
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<Shipment> Shipments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            UserConfiguration.Configure(modelBuilder);
            AddressConfiguration.Configure(modelBuilder);
            FeedbackConfiguration.Configure(modelBuilder);
            CustomerConfiguration.Configure(modelBuilder);
            BusinessOwnerConfiguration.Configure(modelBuilder);
            RiderConfiguration.Configure(modelBuilder);
            RouteConfiguration.Configure(modelBuilder);
            OrderConfiguration.Configure(modelBuilder);
            OrderRiderConfiguration.Configure(modelBuilder);
            PaymentConfiguration.Configure(modelBuilder);
            NotificationConfiguration.Configure(modelBuilder);
            IssuesConfiguration.Configure(modelBuilder);
            RiderAssignmentConfiguration.Configure(modelBuilder);
            OrderRouteConfiguration.Configure(modelBuilder);
            UserRoleConfiguration.Configure(modelBuilder);
            ShipmentConfiguration.Configure(modelBuilder);
        }
    }
}
