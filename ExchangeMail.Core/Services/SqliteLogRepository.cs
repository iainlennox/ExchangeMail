using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteLogRepository : ILogRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteLogRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string level, string source, string message, Exception? ex = null)
    {
        var log = new LogEntity
        {
            Date = DateTime.Now,
            Level = level,
            Source = source,
            Message = message,
            Exception = ex?.ToString()
        };

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<LogEntity> Logs, int TotalCount)> GetLogsAsync(int page, int pageSize)
    {
        var query = _context.Logs.AsQueryable();
        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task ClearLogsAsync()
    {
        // Efficient truncation is tricky in EF Core + SQLite without raw SQL, 
        // but ExecuteSqlRawAsync works well.
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Logs");
    }
}
