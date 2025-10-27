using HealthyApi;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ===================== Database Connection =====================
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===================== Controller and JSON Naming =====================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Use snake_case for JSON input/output
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        o.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
    });

// ===================== CORS Configuration =====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ===================== Swagger Configuration =====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ===================== Middleware Pipeline =====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

// ===================== Startup Logging =====================
var logger = app.Logger;

// Read SQLite connection path
string dbConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=healthy.db";
string dbFilePath = dbConnection.Replace("Data Source=", "").Trim();
string fullDbPath = Path.GetFullPath(dbFilePath);

// Determine current running URL
var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:5000";

// Log information to console
logger.LogInformation("HealthyApi service started.");
logger.LogInformation("API URL: {Urls}", urls);
logger.LogInformation("Database Path: {DbPath}", fullDbPath);
logger.LogInformation("Start Time: {Time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

// Simple console output
Console.WriteLine("========================================");
Console.WriteLine("HealthyApi service started successfully.");
Console.WriteLine($"API URL: {urls}");
Console.WriteLine($"Database Path: {fullDbPath}");
Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("========================================");

app.Run();
