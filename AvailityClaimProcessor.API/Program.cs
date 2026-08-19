using AvailityClaimProcessor.Core.Interfaces;
using AvailityClaimProcessor.Infrastructure.Data;
using AvailityClaimProcessor.Infrastructure.Parsers;
using AvailityClaimProcessor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=availity_claims.db";
builder.Services.AddDbContext<ClaimProcessorDbContext>(options =>
    options.UseSqlite(connectionString));

// Parsers
builder.Services.AddScoped<IEdiParser, Ta1Parser>();
builder.Services.AddScoped<IEdiParser, Parser999>();
builder.Services.AddScoped<IEdiParser, Parser277Ca>();
builder.Services.AddScoped<IEdiParser, Parser835>();
builder.Services.AddScoped<IEdiParser, EbrParser>();
builder.Services.AddScoped<IEdiParser, EbtParser>();

// Services
builder.Services.AddScoped<IStatusMappingService, StatusMappingService>();
builder.Services.AddScoped<EdiProcessingService>();

// FTP background service
builder.Services.Configure<FtpSettings>(builder.Configuration.GetSection("Ftp"));
builder.Services.AddHostedService<FtpMonitoringService>();

builder.Services.AddControllers();

var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimProcessorDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
