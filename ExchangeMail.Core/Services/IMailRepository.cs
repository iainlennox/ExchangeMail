using MimeKit;

namespace ExchangeMail.Core.Services;

public interface IMailRepository
{
    Task SaveMessageAsync(MimeMessage message, string? folderName = null, string? owner = null, bool isImported = false);
    Task SaveMessageWithUserStatesAsync(MimeMessage message, IEnumerable<(string UserEmail, string? Folder)> userStates);
    Task<(IEnumerable<MimeMessage> Messages, int TotalCount)> GetMessagesAsync(string userEmail, string searchString, int page, int pageSize, string folder = "Inbox");
    Task<MimeMessage?> GetMessageAsync(string id, string userEmail);
    Task DeleteMessageAsync(string id, string userEmail);
    Task PermanentDeleteMessageAsync(string id, string userEmail);
    Task MarkAsReadAsync(string id, string userEmail);
    Task MarkAllAsReadAsync(string userEmail, string folder);
    Task EmptyTrashAsync(string userEmail);
    Task CreateFolderAsync(string name, string userEmail);
    Task DeleteFolderAsync(string name, string userEmail);
    Task<IEnumerable<string>> GetFoldersAsync(string userEmail);
    Task MoveMessageAsync(string messageId, string folderName, string userEmail);
    Task CopyMessageAsync(string messageId, string folderName, string userEmail);
    Task<(IEnumerable<MimeMessage> Messages, int TotalCount)> GetAllMessagesAsync(int page, int pageSize);
    Task<(string Id, string From, string Subject)?> GetLatestMessageSummaryAsync(string userEmail, string folder = "Inbox");
    Task UpdateMessageAsync(string id, MimeMessage message);
    Task<Dictionary<string, int>> GetUnreadCountsAsync(string userEmail);
}
