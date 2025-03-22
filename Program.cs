//// Program.cs
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting; // Required for IHost, IHostBuilder, BackgroundService
//using YourNamespace.Data;
//using YourNamespace.Models;

namespace YourNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            //    ////        // Build the host
            //    ////        IHost host = CreateHostBuilder(args).Build();
            //    ////        var dbContext = new  MyDbContext();

            //    ////        // Example: Seed or interact with the database
            //    ////        //using (var scope = host.Services.CreateScope())
            //    ////        //{
            //    ////        //    var services = scope.ServiceProvider;


            //    ////        //    // Ensure the database is created
            //    ////        //    await dbContext.Database.EnsureCreatedAsync();

            //    ////            // Example: Add a customer with an order (seed data)
            //    ////            if (!dbContext.Customers.Any())
            //    ////            {
            //    ////                var customer = new Customer
            //    ////                {
            //    ////                    CustomerName = "John Doe",
            //    ////                    User = new User
            //    ////                    {
            //    ////                        Name = "John Doe",
            //    ////                        Email = "john.doe@example.com",
            //    ////                        Password = "hashedpassword123",
            //    ////                        PhoneNumber = "123-456-7890",
            //    ////                        Address = new Address
            //    ////                        {
            //    ////                            Lang = "en",
            //    ////                            Lat = 40.7128f,
            //    ////                            Area = "New York"
            //    ////                        }
            //    ////                    },
            //    ////                    Orders = new List<Order>
            //    ////                    {
            //    ////                        new Order
            //    ////                        {
            //    ////                            ItemsType = "Electronics",
            //    ////                            Title = "Laptop Delivery",
            //    ////                            IsBreakable = true,
            //    ////                            Notes = "Handle with care",
            //    ////                            Details = "Dell XPS 13",
            //    ////                            Weight = 2.5f,
            //    ////                            Priority = "High",
            //    ////                            State = "Pending",
            //    ////                            OrderPrice = 1200.00m,
            //    ////                            DeliveryPrice = 15.00m,
            //    ////                            Date = DateTime.Now
            //    ////                        }
            //    ////                    }
            //    ////                };
            //    ////                dbContext.Customers.Add(customer);
            //    ////                await dbContext.SaveChangesAsync();
            //    ////                Console.WriteLine("Sample customer with order added to the database.");
            //    ////            //}

            //    ////            // Example: Query customers and their orders
            //    ////            var customers = dbContext.Customers
            //    ////                .Include(c => c.User)
            //    ////                .ThenInclude(u => u.Address)
            //    ////                .Include(c => c.Orders)
            //    ////                .ToList();
            //    ////            foreach (var c in customers)
            //    ////            {
            //    ////                Console.WriteLine($"Customer: {c.CustomerName}, Email: {c.User.Email}, Area: {c.User.Address?.Area}");
            //    ////                foreach (var order in c.Orders)
            //    ////                {
            //    ////                    Console.WriteLine($"  Order: {order.Title}, Price: {order.OrderPrice}, State: {order.State}");
            //    ////                }
            //    ////            }
            //    ////        }

            //    ////        //// Run the host
            //    ////        //await host.RunAsync();
            //    ////    }

            //    ////    static IHostBuilder CreateHostBuilder(string[] args) =>
            //    ////        Host.CreateDefaultBuilder(args)
            //    ////            .ConfigureAppConfiguration((hostingContext, config) =>
            //    ////            {
            //    ////                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            //    ////            })
            //    ////            .ConfigureServices((hostContext, services) =>
            //    ////            {
            //    ////                // Register DbContext with dependency injection
            //    ////                services.AddDbContext<MyDbContext>(options =>
            //    ////                    options.UseSqlServer(
            //    ////                        hostContext.Configuration.GetConnectionString("DefaultConnection")));

            //    ////                // Add hosted service (optional, for console app)
            //    ////                services.AddHostedService<Worker>();
            //    ////            });
            //    ////}

            //    ////// Optional: Worker class for IHostedService (can be removed if not needed)
            //    ////public class Worker : BackgroundService
            //    ////{
            //    ////    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            //    ////    {
            //    ////        while (!stoppingToken.IsCancellationRequested)
            //    ////        {
            //    ////            // Background task logic (optional)
            //    ////            await Task.Delay(1000, stoppingToken);
            //    //        }
        }
    }
}
