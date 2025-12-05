using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using MimeKit;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;

namespace ExchangeMail.Core.Services;

public class SqliteMessageStore : IMessageStore
{
    private readonly IServiceProvider _serviceProvider;

    public SqliteMessageStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        foreach (var segment in buffer)
        {
            stream.Write(segment.Span);
        }
        stream.Position = 0;

        var message = await MimeMessage.LoadAsync(stream, cancellationToken);

        using (var scope = _serviceProvider.CreateScope())
        {
            var mailRepository = scope.ServiceProvider.GetRequiredService<IMailRepository>();
            await mailRepository.SaveMessageAsync(message);

            var notifier = scope.ServiceProvider.GetService<INotifier>();
            if (notifier != null)
            {
                // Assuming the first mailbox is the user for now, or just send a general notification
                var sender = message.From.ToString();
                var subject = message.Subject;
                await notifier.NotifyNewEmailAsync("user", subject, sender);
            }
        }

        return SmtpResponse.Ok;
    }
}
