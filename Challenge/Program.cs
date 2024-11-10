using Microsoft.EntityFrameworkCore;
using Challenge.Data;
using Challenge.Repositories;
using Challenge.Services;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("Challenge.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
Program.ConfigureDatabase(builder.Services, builder.Configuration);

// Register services and repositories
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<IShowRepository, ShowRepository>();

// Register HttpClient
builder.Services.AddHttpClient();

// Configure controllers and API documentation
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
    // NOSONAR: Start
    public static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
            }
            options.UseSqlServer(connectionString);
        });
    }
    // NOSONAR: End

    public void Dummy()
    {
        Console.WriteLine("This is for unit testing");
    }
}
