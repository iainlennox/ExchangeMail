namespace ExchangeMail.Core.Services;

public interface IAiEmailService
{
    Task<string> SummarizeAsync(string content);
    Task<string> GenerateDraftAsync(string prompt);
    Task<string> GetLabelsAsync(string content);
    Task<string> GenerateDailyBriefingAsync(string contextData, string timeOfDay);
}
