using HealthyApi;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using HealthyApi.Helpers;
using System.IO;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===================== Database Connection =====================
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===================== Controller and JSON Naming =====================
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
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
builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations(); //  启用 Swagger 注解
    c.OperationFilter<FileUploadOperation>(); // 允许上传文件接口显示在 Swagger
    c.SupportNonNullableReferenceTypes();
});

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
string dbConnection = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=healthy.db";
string dbFilePath = dbConnection.Replace("Data Source=", "").Trim();
string fullDbPath = Path.GetFullPath(dbFilePath);
string urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "http://localhost:5000";

logger.LogInformation("HealthyApi service started.");
logger.LogInformation("API URL: {Urls}", urls);
logger.LogInformation("Database Path: {DbPath}", fullDbPath);
logger.LogInformation("Start Time: {Time}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("========================================");
Console.WriteLine("HealthyApi service started successfully.");
Console.WriteLine($"API URL: {urls}");
Console.WriteLine($"Database Path: {fullDbPath} (SQLite)");
Console.WriteLine($"Start Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine("========================================");
Console.ResetColor();

app.Run();


// ===================== Swagger File Upload Support =====================
public class FileUploadOperation : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Type == typeof(IFormFile))
            .ToList();

        if (fileParams.Count > 0)
        {
            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParams.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema { Type = "string", Format = "binary" }
                            ),
                            Required = fileParams.Select(p => p.Name).ToHashSet()
                        }
                    }
                }
            };
        }
    }
}
