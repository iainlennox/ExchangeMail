using MimeKit;

namespace ExchangeMail.Core.Services;

public interface IMailRuleService
{
    Task<RuleResult> ApplyRulesAsync(MimeMessage message, string userEmail);
}

public class RuleResult
{
    public string? TargetFolder { get; set; }
    public List<string> Labels { get; set; } = new();
    public bool MarkAsRead { get; set; }
    public bool Flag { get; set; }
    public bool StopProcessing { get; set; }
    public bool Delete { get; set; }
}
