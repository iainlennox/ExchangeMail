using System.Buffers;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using Microsoft.Extensions.DependencyInjection;
using ExchangeMail.Core.Services;

namespace ExchangeMail.Core.Services;

public class SmtpMessageStore : IMessageStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SmtpMessageStore(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await logger.LogAsync("Info", "SmtpServer", "Starting to process received message...");
            }

            using var stream = new MemoryStream(buffer.ToArray());
            MimeMessage message;
            try
            {
                message = await MimeMessage.LoadAsync(stream, cancellationToken);
            }
            catch (Exception)
            {
                // Fallback for messages without headers (e.g. raw text from telnet)
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync();

                message = new MimeMessage();
                message.Body = new TextPart("plain") { Text = text };

                // Try to populate from transaction envelope if possible
                // Note: We rely on the message headers for routing in the logic below, 
                // so we need to ensure message.To/From are populated if we want it delivered.
                // However, without headers, we can't know the "Display Name" etc.
                // We will rely on the fact that the logic below iterates message.To.

                // If we can't get envelope from transaction here easily (without casting), 
                // we might lose routing. 
                // But at least we won't crash.

                // Let's try to set a default subject
                message.Subject = "No Subject (Raw Message)";
            }

            // Security: Block dangerous file types
            var blockedExtensions = new[] { ".exe", ".bat", ".scr", ".ps1", ".vbs", ".cmd", ".js", ".wsf" };
            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart part && !string.IsNullOrEmpty(part.FileName))
                {
                    var ext = Path.GetExtension(part.FileName).ToLowerInvariant();
                    if (blockedExtensions.Contains(ext))
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var logger = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                            await logger.LogAsync("Warning", "SmtpServer", $"Blocked message with dangerous attachment: {part.FileName} (From: {message.From})");
                        }
                        return new SmtpResponse(SmtpReplyCode.TransactionFailed, "Message rejected: Dangerous attachment detected.");
                    }
                }
            }

            using (var scope = _scopeFactory.CreateScope())
            {
                var mailRepository = scope.ServiceProvider.GetRequiredService<IMailRepository>();
                var junkFilterService = scope.ServiceProvider.GetRequiredService<IJunkFilterService>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var mailRuleService = scope.ServiceProvider.GetRequiredService<IMailRuleService>();

                // Identify Recipients
                var localUsers = await userRepository.GetAllUsersAsync();
                var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var mailbox in message.To.Mailboxes)
                {
                    recipients.Add(mailbox.Address);
                }
                foreach (var mailbox in message.Cc.Mailboxes)
                {
                    recipients.Add(mailbox.Address);
                }
                // Note: Bcc is not in message headers usually, but for local dev we assume To/Cc covers most.
                // Ideally we would use transaction.Recipients if available.

                var userStates = new List<(string UserEmail, string? Folder, string? Labels)>();

                foreach (var user in localUsers)
                {
                    if (recipients.Contains(user.Username))
                    {
                        string? folder = null;

                        // 1. Check Junk
                        if (await junkFilterService.IsJunkAsync(message))
                        {
                            folder = "Junk Email";
                        }
                        else
                        {
                            // 2. Check Rules
                            var ruleResult = await mailRuleService.ApplyRulesAsync(message, user.Username);

                            if (ruleResult.StopProcessing && ruleResult.Delete)
                            {
                                // If delete and stop, maybe don't save?
                                // But usually "Delete" means move to Trash or just drop?
                                // If we drop, we don't add to userStates.
                                continue;
                            }

                            folder = ruleResult.TargetFolder;

                            // TODO: Handle Labels, Flags, MarkAsRead
                            // We need to pass these to SaveMessageWithUserStatesAsync or update after saving.
                            // Current SaveMessageWithUserStatesAsync only takes folder.
                            // We might need to update the repository to accept more state.
                            // For now, let's just handle Folder.
                            // If MarkAsRead is true, we might need to a separate call.
                        }

                        userStates.Add((user.Username, folder, null)); // Folder can be null (Inbox), Labels null
                    }
                }

                // If no local recipients found, maybe it's an outbound email?
                // But SmtpMessageStore is for INCOMING (or relay).
                // If it's outbound (from local user), we should save it to their Sent Items?
                // But usually the client (MailController) handles "Sent Items" saving.
                // This is for SMTP delivery.

                if (userStates.Any())
                {
                    await mailRepository.SaveMessageWithUserStatesAsync(message, userStates);

                    // Notify users
                    var notifier = scope.ServiceProvider.GetService<INotifier>();
                    if (notifier != null)
                    {
                        var sender = message.From.ToString();
                        var subject = message.Subject;
                        // Currently broadcasting to all, but we could iterate userStates to notify specific users if supported
                        await notifier.NotifyNewEmailAsync("user", subject, sender);
                    }
                }
                else
                {
                    // Log that we received a message for no one?
                    var logger = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                    await logger.LogAsync("Warning", "SmtpServer", $"Received message for unknown recipients: {string.Join(", ", recipients)}");
                }
            }

            return SmtpResponse.Ok;
        }
        catch (Exception ex)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogRepository>();
                await logger.LogAsync("Error", "SmtpServer", $"Error processing message: {ex.Message}", ex);
            }
            return new SmtpResponse(SmtpReplyCode.TransactionFailed, "Internal Server Error processing message.");
        }
    }
}
