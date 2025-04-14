using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VROOM.Models;
using VROOM.Models.Map;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace VROOM.Data
{
    public class VroomDbContext : IdentityDbContext<User>
    {
        public VroomDbContext(DbContextOptions<VroomDbContext> options)
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
            optionsBuilder.UseLazyLoadingProxies().UseSqlServer("workstation id=VroomDB.mssql.somee.com;packet size=4096;user id=shams22_SQLLogin_1;pwd=pv93y2dob4;data source=VroomDB.mssql.somee.com;persist security info=False;initial catalog=VroomDB;TrustServerCertificate=True");

            base.OnConfiguring(optionsBuilder);
        }
    

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
            modelBuilder.Entity<Address>().HasQueryFilter(a => !a.IsDeleted);

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


            

            base.OnModelCreating(modelBuilder);
        }


    }
}
