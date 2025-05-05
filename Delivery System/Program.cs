using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data.Entity;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddViewOptions(options => {
        options.HtmlHelperOptions.ClientValidationEnabled = true;
    });

builder.Services.AddHttpClient();

builder.Services.AddDbContext<VroomDbContext>(i =>
    i.UseLazyLoadingProxies()
     .UseSqlServer(builder.Configuration.GetConnectionString("DB")));

builder.Services.AddScoped<IMapRepository, MapRepository>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    string apiKey = "prj_live_pk_7735fee5f11db4ba5e136a953720e8ea33a52447";
    if (string.IsNullOrEmpty(apiKey))
    {
        throw new InvalidOperationException("Radar:ApiKey is missing in configuration.");
    }

    var client = new HttpClient();
    client.DefaultRequestHeaders.Add("Authorization", apiKey);
    var dbContext = serviceProvider.GetRequiredService<VroomDbContext>();
    return new MapRepository(client, dbContext);
});

builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<VroomDbContext>();

builder.Services.AddScoped(typeof(RiderRepository));
builder.Services.AddScoped(typeof(AccountManager));
builder.Services.AddScoped(typeof(RoleRepository));
builder.Services.AddScoped(typeof(OrderRepository));
builder.Services.AddScoped(typeof(OrderService));
builder.Services.AddScoped(typeof(UserRepository));
builder.Services.AddScoped(typeof(UserManager<User>));
builder.Services.AddScoped(typeof(SignInManager<User>));
builder.Services.AddScoped(typeof(BaseRepository<>));
builder.Services.AddScoped(typeof(TransactionWork<>));
builder.Services.AddScoped(typeof(AdminServices));
builder.Services.AddScoped(typeof(BusinessOwnerRepository));
builder.Services.AddScoped(typeof(BusinessOwnerService));
builder.Services.AddScoped(typeof(NotificationService));
builder.Services.AddScoped(typeof(NotificationRepository));
builder.Services.AddScoped(typeof(CustomerServices));
builder.Services.AddScoped(typeof(CustomerRepository));
builder.Services.AddScoped(typeof(UserService));
builder.Services.AddScoped(typeof(OrderRiderRepository));
builder.Services.AddScoped(typeof(OrderRouteRepository));
builder.Services.AddScoped(typeof(OrderService));
builder.Services.AddScoped(typeof(RouteServices));
builder.Services.AddScoped(typeof(MapService));
builder.Services.AddScoped(typeof(RouteRepository));
builder.Services.AddScoped(typeof(RouteServices));
builder.Services.AddScoped(typeof(OrderRouteServices));
builder.Services.AddScoped(typeof(ShipmentRepository));
builder.Services.AddScoped(typeof(ShipmentServices));
builder.Services.AddHttpClient();




builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/vroom-admin/account/login"; 
    options.ExpireTimeSpan = TimeSpan.FromDays(30); 
    options.SlidingExpiration = true;
});







var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
	name: "default",
	pattern: "{Controller=account}/{Action=login}/{id?}");

app.MapControllerRoute(

    name: "map",
    pattern: "{Controller=map}/{Action=index}/{id?}");


app.Run();
