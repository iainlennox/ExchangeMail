namespace ExchangeMail.Core.Services;

using ExchangeMail.Core.Data.Entities;

public interface ILogRepository
{
    Task LogAsync(string level, string source, string message, Exception? ex = null);
    Task<(IEnumerable<LogEntity> Logs, int TotalCount)> GetLogsAsync(int page, int pageSize);
    Task ClearLogsAsync();
}
