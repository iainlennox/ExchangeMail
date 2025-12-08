using ExchangeMail.Core.Data;
using ExchangeMail.Core.Services;
using ExchangeMail.Web.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using SmtpServer;
using SmtpServer.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Data Protection (Persist keys to local folder to avoid IIS profile permission issues)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));

// Configure Kestrel for large uploads
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null; // Unlimited
});

// Configure FormOptions
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

// Database
// Database
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    string dbPath = Path.Combine(AppContext.BaseDirectory, "exchangemail.db");
    // Only look for source path if local file doesn't exist and we are in dev
    if (!File.Exists(dbPath) && builder.Environment.IsDevelopment())
    {
        var sourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "exchangemail.db"));
        if (File.Exists(sourcePath))
        {
            dbPath = sourcePath;
        }
    }
    connectionString = $"Data Source={dbPath}";
}

builder.Services.AddDbContext<ExchangeMailContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("ExchangeMail.Web")));

// Register Mail Services
builder.Services.AddScoped<IMailRepository, SqliteMailRepository>();
builder.Services.AddScoped<IUserRepository, SqliteUserRepository>();
builder.Services.AddScoped<IConfigurationService, SqliteConfigurationService>();
builder.Services.AddScoped<ILogRepository, SqliteLogRepository>();
builder.Services.AddScoped<ISafeSenderRepository, SqliteSafeSenderRepository>();
builder.Services.AddScoped<IContactRepository, SqliteContactRepository>();
builder.Services.AddScoped<IBlockListRepository, SqliteBlockListRepository>();
builder.Services.AddScoped<ICalendarRepository, SqliteCalendarRepository>();
builder.Services.AddScoped<ITaskRepository, SqliteTaskRepository>();
builder.Services.AddScoped<IMailRuleRepository, SqliteMailRuleRepository>();
builder.Services.AddScoped<IMailRuleService, MailRuleService>();
builder.Services.AddScoped<IRuleMatcher, RuleMatcher>();
builder.Services.AddScoped<IJunkFilterService, BasicJunkFilterService>();
builder.Services.AddScoped<INotifier, ExchangeMail.Web.Services.SignalRNotifier>();
builder.Services.AddScoped<HtmlSanitizerService>();
builder.Services.AddScoped<PstImportService>();
builder.Services.AddSingleton<ImportStatusService>();
builder.Services.AddSingleton<SmtpServer.Storage.IMessageStore, SmtpMessageStore>();
builder.Services.AddScoped<IAiEmailService, AiEmailService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<SmtpHostedService>();
// builder.Services.AddHostedService<ExchangeMail.Web.Services.Imap.ImapHostedService>();

var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExchangeMailContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Configure the HTTP request pipeline.
// Always show detailed errors for debugging deployment
app.UseDeveloperExceptionPage();
/*
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
*/

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<ExchangeMail.Web.Hubs.MailHub>("/mailHub");

app.Run();
