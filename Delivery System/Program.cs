using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<VroomDbContext>(i =>
    i.UseLazyLoadingProxies()
     .UseSqlServer(builder.Configuration.GetConnectionString("DB")));

builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<VroomDbContext>();

builder.Services.AddScoped(typeof(RiderRepository));
builder.Services.AddScoped(typeof(AccountManager));
builder.Services.AddScoped(typeof(RoleRepository));
builder.Services.AddScoped(typeof(OrderRepository));
builder.Services.AddScoped(typeof(OrderService));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Rider}/{action=GetAll}/{id?}");

app.Run();
