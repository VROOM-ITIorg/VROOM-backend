using Microsoft.EntityFrameworkCore;
using VROOM.Data;
using VROOM.Repositories;
using VROOM.Repositories.VROOM.Repositories;
using VROOM.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register the DbContext (assuming VROOMContext is your DbContext implementation)
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<RiderRepository>();

// Register services
builder.Services.AddScoped<RiderService>();

// Add Swagger (since the stack trace shows Swashbuckle)
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();