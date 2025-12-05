using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteMailRuleRepository : IMailRuleRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteMailRuleRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MailRuleEntity>> GetRulesAsync(string userEmail)
    {
        return await _context.MailRules
            .Include(r => r.Conditions)
            .Include(r => r.Actions)
            .Where(r => r.UserEmail == userEmail)
            .ToListAsync();
    }

    public async Task AddRuleAsync(MailRuleEntity rule)
    {
        _context.MailRules.Add(rule);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateRuleAsync(MailRuleEntity rule)
    {
        _context.MailRules.Update(rule);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRuleAsync(int id)
    {
        var rule = await _context.MailRules.FindAsync(id);
        if (rule != null)
        {
            _context.MailRules.Remove(rule);
            await _context.SaveChangesAsync();
        }
    }
}
