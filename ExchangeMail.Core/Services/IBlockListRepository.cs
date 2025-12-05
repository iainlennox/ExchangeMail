namespace ExchangeMail.Core.Services;

public interface IBlockListRepository
{
    Task<bool> IsBlockedAsync(string email);
    Task AddBlockedSenderAsync(string email);
    Task RemoveBlockedSenderAsync(string email);
}
