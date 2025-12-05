using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteBlockListRepository : IBlockListRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteBlockListRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<bool> IsBlockedAsync(string email)
    {
        return await _context.BlockedSenders.AnyAsync(s => s.Email.ToLower() == email.ToLower());
    }

    public async Task AddBlockedSenderAsync(string email)
    {
        if (await IsBlockedAsync(email)) return;

        _context.BlockedSenders.Add(new BlockedSenderEntity { Email = email });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveBlockedSenderAsync(string email)
    {
        var entity = await _context.BlockedSenders.FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());
        if (entity != null)
        {
            _context.BlockedSenders.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
