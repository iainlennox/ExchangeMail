using ExchangeMail.Core.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace ExchangeMail.Tests.Services;

public class LogReaderTests
{
    private readonly ITestOutputHelper _output;

    public LogReaderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ReadLogs()
    {
        var dbPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ExchangeMail.Web", "exchangemail.db"));
        var connectionString = $"Data Source={dbPath}";
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseSqlite(connectionString)
            .Options;

        using var context = new ExchangeMailContext(options);
        // Ensure we can access the DB
        if (!File.Exists(dbPath))
        {
            _output.WriteLine($"Database not found at: {dbPath}");
            return;
        }

        var logs = context.Logs.OrderByDescending(l => l.Id).Take(10).ToList();

        foreach (var log in logs)
        {
            _output.WriteLine($"[{log.Timestamp}] {log.Level} - {log.Category}: {log.Message}");
        }
    }
}
