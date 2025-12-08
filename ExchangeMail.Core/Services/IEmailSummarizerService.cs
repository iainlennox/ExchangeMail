namespace ExchangeMail.Core.Services;

public interface IEmailSummarizerService
{
    Task<string> SummarizeAsync(string content);
}
