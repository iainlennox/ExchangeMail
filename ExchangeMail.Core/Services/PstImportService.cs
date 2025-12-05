using XstReader;
using MimeKit;
using System.Linq;

namespace ExchangeMail.Core.Services;

public class PstImportService
{
    private readonly IMailRepository _mailRepository;
    private readonly ImportStatusService _importStatusService;

    public PstImportService(IMailRepository mailRepository, ImportStatusService importStatusService)
    {
        _mailRepository = mailRepository;
        _importStatusService = importStatusService;
    }

    public async Task<(int TotalMessages, string TempFilePath, List<string> FolderDebugInfo)> AnalyzePstAsync(Stream pstStream)
    {
        var tempFilePath = Path.GetTempFileName();
        using (var fileStream = File.Create(tempFilePath))
        {
            await pstStream.CopyToAsync(fileStream);
        }

        int totalMessages = 0;
        var folderDebugInfo = new List<string>();

        using (var xstFile = new XstFile(tempFilePath))
        {
            totalMessages = CountMessages(xstFile.RootFolder, folderDebugInfo);
        }

        return (totalMessages, tempFilePath, folderDebugInfo);
    }

    private int CountMessages(XstFolder folder, List<string> debugInfo, int depth = 0)
    {
        int count = 0;
        try
        {
            count = folder.Messages.Count();
        }
        catch
        {
            count = folder.ContentCount;
        }

        var indent = new string('-', depth * 2);
        debugInfo.Add($"{indent}{folder.DisplayName} (Messages: {count}, ContentCount: {folder.ContentCount})");

        foreach (var subFolder in folder.Folders)
        {
            count += CountMessages(subFolder, debugInfo, depth + 1);
        }
        return count;
    }

    private class ProgressTracker
    {
        public int Count { get; set; }
    }

    public async Task ImportPstAsync(string tempFilePath, string userEmail, string jobId)
    {
        try
        {
            using (var xstFile = new XstFile(tempFilePath))
            {
                var rootFolder = xstFile.RootFolder;
                var tracker = new ProgressTracker();
                await ProcessFolderAsync(rootFolder, userEmail, jobId, tracker);
                _importStatusService.CompleteJob(jobId);
            }
        }
        catch (Exception ex)
        {
            _importStatusService.FailJob(jobId, ex.Message);
            throw;
        }
        finally
        {
            // XstReader appears to have a bug where it accesses the file on a background thread
            // even after the XstFile object is disposed. Deleting the file immediately causes
            // a FileNotFoundException on that background thread, crashing the application.
            // We leave the temp file to be cleaned up by the OS or a maintenance task.
            /*
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
            */
        }
    }

    private async Task ProcessFolderAsync(XstFolder folder, string userEmail, string jobId, ProgressTracker tracker, string? parentPath = null)
    {
        var currentPath = parentPath == null ? folder.DisplayName : $"{parentPath}/{folder.DisplayName}";

        string? targetFolder = null;

        if (folder.ContentCount > 0)
        {
            if (string.Equals(folder.DisplayName, "Top of Outlook data file", StringComparison.OrdinalIgnoreCase))
            {
                targetFolder = null;
            }
            else if (string.Equals(folder.DisplayName, "Inbox", StringComparison.OrdinalIgnoreCase))
            {
                targetFolder = "Inbox";
            }
            else if (string.Equals(folder.DisplayName, "Deleted Items", StringComparison.OrdinalIgnoreCase))
            {
                targetFolder = "Deleted Items";
            }
            else if (string.Equals(folder.DisplayName, "Sent Items", StringComparison.OrdinalIgnoreCase))
            {
                targetFolder = "Sent Items";
            }
            else
            {
                targetFolder = folder.DisplayName;
                await _mailRepository.CreateFolderAsync(targetFolder, userEmail);
            }

            foreach (var message in folder.Messages)
            {
                try
                {
                    var mimeMessage = ConvertToMimeMessage(message);
                    if (mimeMessage != null)
                    {
                        string? dbFolder = null;

                        if (targetFolder == "Inbox") dbFolder = null;
                        else dbFolder = targetFolder;

                        await _mailRepository.SaveMessageAsync(mimeMessage, dbFolder, userEmail, isImported: true);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to import message: {ex.Message}");
                }

                tracker.Count++;
                if (tracker.Count % 10 == 0) // Update every 10 items to reduce contention
                {
                    _importStatusService.UpdateProgress(jobId, tracker.Count);
                }
            }
            // Ensure progress is updated at end of folder
            _importStatusService.UpdateProgress(jobId, tracker.Count);
        }

        foreach (var subFolder in folder.Folders)
        {
            await ProcessFolderAsync(subFolder, userEmail, jobId, tracker, currentPath);
        }
    }

    private MimeMessage? ConvertToMimeMessage(XstMessage xstMessage)
    {
        try
        {
            var mimeMessage = new MimeMessage();

            mimeMessage.Subject = xstMessage.Subject;

            if (xstMessage.Date.HasValue)
            {
                mimeMessage.Date = new DateTimeOffset(xstMessage.Date.Value);
            }
            else
            {
                mimeMessage.Date = DateTimeOffset.Now;
            }

            if (!string.IsNullOrEmpty(xstMessage.From))
            {
                try
                {
                    mimeMessage.From.Add(MailboxAddress.Parse(xstMessage.From));
                }
                catch
                {
                    mimeMessage.From.Add(new MailboxAddress(xstMessage.From, ""));
                }
            }

            // To
            if (!string.IsNullOrEmpty(xstMessage.To))
            {
                foreach (var address in xstMessage.To.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        mimeMessage.To.Add(MailboxAddress.Parse(address.Trim()));
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            // Cc
            if (!string.IsNullOrEmpty(xstMessage.Cc))
            {
                foreach (var address in xstMessage.Cc.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    try
                    {
                        mimeMessage.Cc.Add(MailboxAddress.Parse(address.Trim()));
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            var builder = new BodyBuilder();
            // Check if Body is null before accessing properties
            if (xstMessage.Body != null)
            {
                var textBody = xstMessage.Body.Text;

                // Heuristic: If the text body looks like HTML, treat it as HTML
                if (!string.IsNullOrEmpty(textBody) &&
                   (textBody.TrimStart().StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                    textBody.TrimStart().StartsWith("<html", StringComparison.OrdinalIgnoreCase)))
                {
                    builder.HtmlBody = textBody;
                    // Also set TextBody to a stripped version? 
                    // For now, let's just set HtmlBody. MimeKit might auto-generate TextBody or we can leave it null.
                }
                else
                {
                    builder.TextBody = textBody;
                }
            }

            foreach (var attachment in xstMessage.Attachments)
            {
                if (attachment.IsFile)
                {
                    // Skipping attachments for now as planned
                }
            }

            mimeMessage.Body = builder.ToMessageBody();

            return mimeMessage;
        }
        catch
        {
            return null;
        }
    }
}
