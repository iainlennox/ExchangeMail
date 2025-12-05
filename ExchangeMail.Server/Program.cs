using ExchangeMail.Core.Data;
using ExchangeMail.Core.Services;
using ExchangeMail.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;

var builder = Host.CreateApplicationBuilder(args);

// Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "ExchangeMailServer";
});

// Database
// Database
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    string dbPath = Path.Combine(AppContext.BaseDirectory, "exchangemail.db");
    if (!File.Exists(dbPath) && builder.Environment.IsDevelopment())
    {
        var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ExchangeMail.Web", "exchangemail.db"));
        if (File.Exists(sourcePath))
        {
            dbPath = sourcePath;
        }
    }
    connectionString = $"Data Source={dbPath}";
}

Console.WriteLine($"[DIAGNOSTIC] Using Connection String: {connectionString}");

builder.Services.AddDbContext<ExchangeMailContext>(options =>
    options.UseSqlite(connectionString));

// Core Services
builder.Services.AddScoped<IConfigurationService, SqliteConfigurationService>();

// Hosted Services
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<HeartbeatService>();

var host = builder.Build();

// Ensure DB is created
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExchangeMailContext>();
    // Ensure migrations are applied. 
    // Note: Server might not have migrations assembly. It relies on Web to apply them usually, 
    // but if we want it to be standalone, we should use Migrate(). 
    // However, if Server doesn't reference the migrations assembly, Migrate() might fail or do nothing if it can't find migrations.
    // Given the shared DB, it's safer to just let Web handle migrations or ensure Server has access.
    // For now, let's remove EnsureCreated to avoid the crash.
    // db.Database.Migrate(); 
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

host.Run();
