using MimeKit;

namespace ExchangeMail.Core.Services;

public interface IJunkFilterService
{
    Task<bool> IsJunkAsync(MimeMessage message);
}
