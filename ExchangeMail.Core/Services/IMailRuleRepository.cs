using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface IMailRuleRepository
{
    Task<IEnumerable<MailRuleEntity>> GetRulesAsync(string userEmail);
    Task AddRuleAsync(MailRuleEntity rule);
    Task UpdateRuleAsync(MailRuleEntity rule);
    Task DeleteRuleAsync(int id);
}
