using AvailityClaimResponseProcessor.Core.Interfaces;
using AvailityClaimResponseProcessor.Infrastructure.Data;
using AvailityClaimResponseProcessor.Infrastructure.Ftp;
using AvailityClaimResponseProcessor.Infrastructure.Parsers;
using AvailityClaimResponseProcessor.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ClaimDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositories ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();

// ── EDI Parsers ───────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IStatusMappingService, StatusMappingService>();
builder.Services.AddScoped<IEdiParser, Ta1Parser>();
builder.Services.AddScoped<IEdiParser, Acknowledgment999Parser>();
builder.Services.AddScoped<IEdiParser, ClaimAcknowledgment277CaParser>();
builder.Services.AddScoped<IEdiParser, RemittanceAdvice835Parser>();

// ── FTP ───────────────────────────────────────────────────────────────────────
builder.Services.Configure<FtpSettings>(builder.Configuration.GetSection("FtpSettings"));
builder.Services.AddSingleton<IFtpService, FtpService>();

// ── Background Service ────────────────────────────────────────────────────────
builder.Services.AddHostedService<EdiFileProcessingService>();

// ── API ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ── Migrate DB on startup ────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClaimDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program { }

