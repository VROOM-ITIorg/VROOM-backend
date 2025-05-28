using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Repository;
using VROOM.Services;
using System.Text.Json.Serialization;
using Hangfire;
using API.Myhubs;

// using Serilog;
//using VROOM.Services.Mapping;



// Log.Information("Logger configured.");



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowAll", policy =>
//    {
//        policy.AllowAnyOrigin()
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .SetIsOriginAllowed(url => true)
              .AllowCredentials();
    });
});

// builder.Host.UseSerilog();
// Log.Logger = new LoggerConfiguration()
//     .ReadFrom.Configuration(builder.Configuration)
//     .Enrich.FromLogContext()
//     .CreateLogger();


// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    })
    .AddNewtonsoftJson(options =>
     {
         options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore; // For Newtonsoft.Json
     });

builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "VROOM API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field (e.g., Bearer <token>)",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Configure DbContext with lazy loading
builder.Services.AddDbContext<VroomDbContext>(options =>
    options
        .UseSqlServer(builder.Configuration.GetConnectionString("DB"))
        .UseLazyLoadingProxies());

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<VroomDbContext>()
    .AddDefaultTokenProviders();

// Add services to the container.

//builder.Services.AddControllers();

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DB")));

// Add Hangfire server to process background jobs
builder.Services.AddHangfireServer();
builder.Services.AddHttpClient();
//builder.Services.AddDbContext<VroomDbContext>
//    (i => i.UseLazyLoadingProxies().UseSqlServer(builder.Configuration.GetConnectionString("DB")));
//builder.Services.AddIdentity<User, IdentityRole>()
//    .AddEntityFrameworkStores<VroomDbContext>();
builder.Services.AddScoped(typeof(RiderRepository));
builder.Services.AddScoped(typeof(RoleRepository));
builder.Services.AddScoped(typeof(AccountManager));
builder.Services.AddScoped(typeof(OrderRepository));
builder.Services.AddScoped(typeof(IssuesRepository));
builder.Services.AddScoped<OrderRiderRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<CustomerServices>();
builder.Services.AddScoped<RiderService>();
builder.Services.AddScoped<BusinessOwnerRepository>();
builder.Services.AddScoped<BusinessOwnerService>();
builder.Services.AddScoped<RouteRepository>();
builder.Services.AddScoped<RouteServices>();
builder.Services.AddScoped<OrderRouteRepository>();
builder.Services.AddScoped<OrderRouteServices>();
builder.Services.AddScoped<ShipmentRepository>();
builder.Services.AddScoped<ShipmentServices>();
builder.Services.AddScoped(typeof(OrderService));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IssueService>();




//builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);


// Configure JWT Authentication
var jwtSecret = "ShampooShampooShampooShampooShampooShampoo";
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 16)
{
    throw new InvalidOperationException("JWT Secret is missing or too short in configuration. It must be at least 16 characters long.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "VROOM",
        ValidAudience = "VROOM",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "VROOM API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the root (e.g., https://localhost:5169/)
    });
}

app.UseHttpsRedirection();

// Custom middleware to log request body for /api/user/register
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/user/register") && context.Request.Method == "POST")
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        Console.WriteLine($"Request Body: {body}");
        context.Request.Body.Position = 0; // Reset the stream position
    }
    await next();
});


//app.UseAuthentication();
//app.UseAuthorization();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VROOM API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the root (e.g., https://localhost:5001/)
});
//app.UseAuthorization();
app.UseCors();
app.UseStaticFiles();
app.UseRouting();

app.MapHub<AcceptOrderHub>("/AcceptRejectOrders");

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=index}");


// Schedule the recurring job when the application starts
RecurringJob.AddOrUpdate<OrderService>(
    "track-order-job",
    service => service.TrackOrdersAsync(), // Replace with actual job
    "*/30 * * * * *"); // Every 30 seconds


// Seed roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { RoleConstants.Customer, RoleConstants.BusinessOwner, RoleConstants.Rider, RoleConstants.Admin };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
// Log.Information("Application starting...");

app.Run();