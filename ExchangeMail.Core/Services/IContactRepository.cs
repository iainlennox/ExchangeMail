using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface IContactRepository
{
    Task<IEnumerable<ContactEntity>> SearchContactsAsync(string query);
    Task AddContactAsync(ContactEntity contact);
    Task<IEnumerable<ContactEntity>> GetAllContactsAsync();
}
