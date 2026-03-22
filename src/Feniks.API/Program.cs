using Microsoft.EntityFrameworkCore;
using Feniks.Shared.Data;
using System.Text.Json.Serialization;
using Feniks.API.Services;  // Добавлен using для ReportService

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<FeniksDbContext>(options =>
    options.UseSqlServer(
        "Server=.\\SQLEXPRESS;Database=FeniksDB;Trusted_Connection=True;TrustServerCertificate=True;",
        b => b.MigrationsAssembly("Feniks.API")));

// Добавьте настройки JSON для предотвращения циклических ссылок
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

// Регистрация сервисов
builder.Services.AddScoped<ReportService>();  // Регистрируем ReportService

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for Blazor
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// Create database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeniksDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("✅ База данных FeniksDB готова");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.MapControllers();
app.MapGet("/", () => "Feniks API работает!");

app.Run("http://localhost:5050");