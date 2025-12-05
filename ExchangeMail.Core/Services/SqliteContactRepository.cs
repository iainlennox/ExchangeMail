using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteContactRepository : IContactRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteContactRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ContactEntity>> SearchContactsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<ContactEntity>();
        }

        query = query.ToLower();
        return await _context.Contacts
            .Where(c => c.Name.ToLower().Contains(query) || c.Email.ToLower().Contains(query))
            .ToListAsync();
    }

    public async Task AddContactAsync(ContactEntity contact)
    {
        _context.Contacts.Add(contact);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ContactEntity>> GetAllContactsAsync()
    {
        return await _context.Contacts.ToListAsync();
    }
}
