using MimeKit;

namespace ExchangeMail.Core.Services;

public class BasicJunkFilterService : IJunkFilterService
{
    private readonly ISafeSenderRepository _safeSenderRepository;
    private readonly IBlockListRepository _blockListRepository;

    public BasicJunkFilterService(ISafeSenderRepository safeSenderRepository, IBlockListRepository blockListRepository)
    {
        _safeSenderRepository = safeSenderRepository;
        _blockListRepository = blockListRepository;
    }

    public async Task<bool> IsJunkAsync(MimeMessage message)
    {
        var sender = message.From.Mailboxes.FirstOrDefault()?.Address;
        if (string.IsNullOrEmpty(sender)) return false;

        // 1. Check Safe Senders (Allow list takes precedence)
        if (await _safeSenderRepository.IsSafeSenderAsync(sender))
        {
            return false;
        }

        // 2. Check Block List
        if (await _blockListRepository.IsBlockedAsync(sender))
        {
            return true;
        }

        // 3. (Future) Content analysis / heuristics

        return false;
    }
}
