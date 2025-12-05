using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface ISafeSenderRepository
{
    Task<bool> IsSafeSenderAsync(string email);
    Task AddSafeSenderAsync(string email);
    Task RemoveSafeSenderAsync(string email);
}
