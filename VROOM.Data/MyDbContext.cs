using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VROOM.Models;

namespace VROOM.Data
{
    public class MyDbContext : IdentityDbContext<User>
    {
        //private readonly IConfiguration _configuration;

        //public MyDbContext(DbContextOptions<MyDbContext> options, IConfiguration configuration)
        //    : base(options)
        //{
        //    _configuration = configuration;
        //}

        public MyDbContext()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies().UseSqlServer("Data source = .; Initial catalog = DataBase_Manager; Integrated security= true; trustservercertificate = true;MultipleActiveResultSets=True");
            base.OnConfiguring(optionsBuilder);

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
        //SOS
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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


            //modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
