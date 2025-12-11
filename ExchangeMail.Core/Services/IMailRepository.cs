using MimeKit;

namespace ExchangeMail.Core.Services;

public interface IMailRepository
{
    Task SaveMessageAsync(MimeMessage message, string? folderName = null, string? owner = null, bool isImported = false);
    Task SaveMessageWithUserStatesAsync(MimeMessage message, IEnumerable<(string UserEmail, string? Folder, string? Labels)> userStates);
    Task<(IEnumerable<MimeMessage> Messages, int TotalCount)> GetMessagesAsync(string userEmail, string searchString, int page, int pageSize, string folder = "Inbox", bool? isFocused = null, string sort = "Date", string filter = "All", bool sortDesc = true);
    Task<MimeMessage?> GetMessageAsync(string id, string userEmail);
    Task DeleteMessageAsync(string id, string userEmail);
    Task PermanentDeleteMessageAsync(string id, string userEmail);
    Task MarkAsReadAsync(string id, string userEmail);
    Task MarkAsUnreadAsync(string id, string userEmail);
    Task MarkAllAsReadAsync(string userEmail, string folder);
    Task EmptyTrashAsync(string userEmail);
    Task CreateFolderAsync(string name, string userEmail);
    Task DeleteFolderAsync(string name, string userEmail);
    Task<IEnumerable<string>> GetFoldersAsync(string userEmail);
    Task MoveMessageAsync(string messageId, string folderName, string userEmail);
    Task CopyMessageAsync(string messageId, string folderName, string userEmail);
    Task UpdateMessageLabelsAsync(string messageId, string userId, string labels);
    Task<(IEnumerable<MimeMessage> Messages, int TotalCount)> GetAllMessagesAsync(int page, int pageSize);
    Task<(string Id, string From, string Subject)?> GetLatestMessageSummaryAsync(string userEmail, string folder = "Inbox");
    Task UpdateMessageAsync(string id, MimeMessage message);
    Task<Dictionary<string, int>> GetUnreadCountsAsync(string userEmail);
    Task<IEnumerable<MimeMessage>> GetThreadMessagesAsync(string threadId, string userEmail);
    Task SetMessageFocusedAsync(string messageId, string userEmail, bool isFocused);
    Task RepairThreadsAsync();
}
