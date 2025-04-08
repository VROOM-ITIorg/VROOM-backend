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

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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

builder.Services.AddControllers();
//builder.Services.AddDbContext<VroomDbContext>
//    (i => i.UseLazyLoadingProxies().UseSqlServer(builder.Configuration.GetConnectionString("DB")));
//builder.Services.AddIdentity<User, IdentityRole>()
//    .AddEntityFrameworkStores<VroomDbContext>();
builder.Services.AddScoped(typeof(RiderRepository));
builder.Services.AddScoped(typeof(RoleRepository));
builder.Services.AddScoped(typeof(AccountManager));
builder.Services.AddScoped(typeof(OrderRepository));
builder.Services.AddScoped(typeof(OrderService));
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserService>();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
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

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();


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


app.UseAuthentication();
app.UseAuthorization();

// Enable Swagger middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VROOM API v1");
    c.RoutePrefix = string.Empty; // Set Swagger UI at the root (e.g., https://localhost:5001/)
});
//app.UseAuthorization();

app.UseStaticFiles();
app.UseRouting();
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