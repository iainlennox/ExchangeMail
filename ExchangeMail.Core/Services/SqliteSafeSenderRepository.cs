using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteSafeSenderRepository : ISafeSenderRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteSafeSenderRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<bool> IsSafeSenderAsync(string email)
    {
        return await _context.SafeSenders.AnyAsync(s => s.Email.ToLower() == email.ToLower());
    }

    public async Task AddSafeSenderAsync(string email)
    {
        if (!await IsSafeSenderAsync(email))
        {
            _context.SafeSenders.Add(new SafeSenderEntity { Email = email });
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveSafeSenderAsync(string email)
    {
        var sender = await _context.SafeSenders.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
        if (sender != null)
        {
            _context.SafeSenders.Remove(sender);
            await _context.SaveChangesAsync();
        }
    }
}
