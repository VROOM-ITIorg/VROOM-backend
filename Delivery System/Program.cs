using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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


builder.Services.AddDbContext<VroomDbContext>(i =>
    i.UseLazyLoadingProxies()
     .UseSqlServer(builder.Configuration.GetConnectionString("DB")));

builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<VroomDbContext>();

builder.Services.AddScoped(typeof(RiderRepository));
builder.Services.AddScoped(typeof(AccountManager));
builder.Services.AddScoped(typeof(RoleRepository));
builder.Services.AddScoped(typeof(OrderRepository));
builder.Services.AddScoped(typeof(OrderService));
builder.Services.AddScoped(typeof(AdminServices));
builder.Services.AddScoped(typeof(UserRepository));
builder.Services.AddScoped(typeof(UserManager<User>));
builder.Services.AddScoped(typeof(SignInManager<User>));
builder.Services.AddScoped(typeof(BaseRepository<>));
builder.Services.AddScoped(typeof(TransactionWork<>));
builder.Services.AddScoped(typeof(BusinessOwnerRepository));
builder.Services.AddScoped(typeof(BusinessOwnerService));

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

app.UseStaticFiles();


app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "vroom-admin/{Controller=account}/{Action=Login}/{id?}");



app.Run();
