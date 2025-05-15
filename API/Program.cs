using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Models;
using VROOM.Repositories;
using VROOM.Services;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using VROOM.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Configure SignalR
builder.Services.AddSignalR();

// Configure CORS to allow Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // URL بتاع الـ Angular
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // لازم عشان SignalR مع التوثيق
    });
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DB"))
           .UseLazyLoadingProxies());

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<VroomDbContext>()
    .AddDefaultTokenProviders();

// Add Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DB")));
builder.Services.AddHangfireServer();

// Add repositories and services
builder.Services.AddHttpClient();
builder.Services.AddScoped<RiderRepository>();
builder.Services.AddScoped<RoleRepository>();
builder.Services.AddScoped<AccountManager>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<IssuesRepository>();
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
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IssueService>();
builder.Services.AddScoped<ConcurrentDictionary<string, ShipmentConfirmation>>();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Key"] ?? "ShampooShampooShampooShampooShampooShampoo";
if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 16)
{
    throw new InvalidOperationException("JWT Secret is missing or too short. It must be at least 16 characters long.");
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "VROOM",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "VROOM",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };

    // Handle JWT token for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/riderHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Apply CORS before routing and authentication
app.UseCors("AllowAngularApp"); // لازم تكون قبل UseRouting
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard();

// Map SignalR Hub
app.MapHub<ClientHub>("/riderHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=index}");

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

app.Run();