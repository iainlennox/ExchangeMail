using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace ExchangeMail.Core.Services;

public class SqliteMailRepository : IMailRepository
{
    private readonly ExchangeMailContext _context;
    private readonly ILogRepository _logRepository;

    public SqliteMailRepository(ExchangeMailContext context, ILogRepository logRepository)
    {
        _context = context;
        _logRepository = logRepository;
    }

    public async Task SaveMessageAsync(MimeMessage message, string? folderName = null, string? owner = null, bool isImported = false)
    {
        var userStates = new List<(string UserEmail, string? Folder)>();

        if (!string.IsNullOrEmpty(owner))
        {
            userStates.Add((owner, folderName));
        }
        else
        {
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                if (message.To.ToString().Contains(user.Username) || (message.Cc != null && message.Cc.ToString().Contains(user.Username)))
                {
                    userStates.Add((user.Username, folderName));
                }
            }
        }

        await SaveMessageWithUserStatesAsync(message, userStates);
    }

    public async Task SaveMessageWithUserStatesAsync(MimeMessage message, IEnumerable<(string UserEmail, string? Folder)> userStates)
    {
        await _logRepository.LogAsync("Info", "Repository", $"Saving message from {message.From} for {userStates.Count()} users.");

        using var memoryStream = new MemoryStream();
        await message.WriteToAsync(memoryStream);
        var rawContent = memoryStream.ToArray();

        var messageId = Guid.NewGuid().ToString();
        var messageEntity = new MessageEntity
        {
            Id = messageId,
            From = message.From.ToString(),
            To = message.To.ToString(),
            Subject = message.Subject ?? "(No Subject)",
            Date = message.Date.DateTime,
            RawContent = rawContent,
            IsImported = false
        };

        _context.Messages.Add(messageEntity);

        foreach (var state in userStates)
        {
            _context.UserMessages.Add(new UserMessageEntity
            {
                UserId = state.UserEmail,
                MessageId = messageId,
                Folder = state.Folder,
                IsRead = false,
                IsDeleted = false
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<MimeMessage> Messages, int TotalCount)> GetMessagesAsync(string userEmail, string searchString, int page, int pageSize, string folder = "Inbox")
    {
        var query = from um in _context.UserMessages
                    join m in _context.Messages on um.MessageId equals m.Id
                    where um.UserId == userEmail
                    select new { um, m };

        if (folder.Equals("Deleted Items", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.um.IsDeleted);
        }
        else if (folder.Equals("Inbox", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.um.IsDeleted && x.um.Folder == null);
        }
        else
        {
            query = query.Where(x => !x.um.IsDeleted && x.um.Folder == folder);
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            searchString = searchString.ToLower();
            query = query.Where(x => x.m.Subject.ToLower().Contains(searchString)
                                  || x.m.From.ToLower().Contains(searchString)
                                  || x.m.To.ToLower().Contains(searchString));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.m.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var messages = new List<MimeMessage>();
        foreach (var item in items)
        {
            using var stream = new MemoryStream(item.m.RawContent);
            var message = await MimeMessage.LoadAsync(stream);

            message.Headers.Add("X-Db-Id", item.m.Id);

            if (item.um.IsRead)
            {
                message.Headers.Add("X-Is-Read", "true");
            }
            messages.Add(message);
        }

        return (messages, totalCount);
    }

    public async Task MarkAsReadAsync(string id, string userEmail)
    {
        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == userEmail);
        if (userMessage != null)
        {
            userMessage.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userEmail, string folder)
    {
        var query = _context.UserMessages.Where(um => um.UserId == userEmail && !um.IsRead);

        if (folder.Equals("Deleted Items", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(um => um.IsDeleted);
        }
        else if (folder.Equals("Inbox", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(um => !um.IsDeleted && um.Folder == null);
        }
        else
        {
            query = query.Where(um => !um.IsDeleted && um.Folder == folder);
        }

        var unreadMessages = await query.ToListAsync();
        if (unreadMessages.Any())
        {
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<MimeMessage?> GetMessageAsync(string id, string userEmail)
    {
        var messageEntity = await _context.Messages.FindAsync(id);
        if (messageEntity == null) return null;

        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == userEmail);
        // If userMessage is null, the user might not have access or it's a global message?
        // For now, assume if no UserMessage, return message without state (or null if strict).
        // Let's return the message but without state headers if userMessage is missing.

        using var stream = new MemoryStream(messageEntity.RawContent);
        var message = await MimeMessage.LoadAsync(stream);

        message.Headers.Add("X-Db-Id", messageEntity.Id);

        if (userMessage != null)
        {
            if (userMessage.IsDeleted)
            {
                message.Headers.Add("X-Is-Deleted", "true");
            }

            if (!string.IsNullOrEmpty(userMessage.Folder))
            {
                message.Headers.Add("X-Folder", userMessage.Folder);
            }

            if (userMessage.IsRead)
            {
                message.Headers.Add("X-Is-Read", "true");
            }
        }

        return message;
    }

    public async Task DeleteMessageAsync(string id, string userEmail)
    {
        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == userEmail);
        if (userMessage != null)
        {
            userMessage.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task PermanentDeleteMessageAsync(string id, string userEmail)
    {
        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == userEmail);
        if (userMessage != null)
        {
            _context.UserMessages.Remove(userMessage);
            // Check if any other users reference this message. If not, delete MessageEntity?
            // For now, keep MessageEntity to avoid complexity.
            await _context.SaveChangesAsync();
        }
    }

    public async Task EmptyTrashAsync(string userEmail)
    {
        var messagesToDelete = await _context.UserMessages
            .Where(um => um.UserId == userEmail && um.IsDeleted)
            .ToListAsync();

        if (messagesToDelete.Any())
        {
            _context.UserMessages.RemoveRange(messagesToDelete);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CreateFolderAsync(string name, string userEmail)
    {
        if (await _context.Folders.AnyAsync(f => f.Name == name && f.UserEmail == userEmail))
        {
            return;
        }

        _context.Folders.Add(new FolderEntity
        {
            Name = name,
            UserEmail = userEmail
        });
        await _context.SaveChangesAsync();
    }

    public async Task DeleteFolderAsync(string name, string userEmail)
    {
        var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Name == name && f.UserEmail == userEmail);
        if (folder != null)
        {
            var messages = await _context.UserMessages
                .Where(um => um.UserId == userEmail && um.Folder == name)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.Folder = null;
                message.IsDeleted = true;
            }

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<string>> GetFoldersAsync(string userEmail)
    {
        return await _context.Folders
            .Where(f => f.UserEmail == userEmail)
            .Select(f => f.Name)
            .ToListAsync();
    }

    public async Task MoveMessageAsync(string messageId, string folderName, string userEmail)
    {
        folderName = System.Net.WebUtility.UrlDecode(folderName);
        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == messageId && um.UserId == userEmail);

        if (userMessage != null)
        {
            if (folderName == "Inbox")
            {
                userMessage.Folder = null;
                userMessage.IsDeleted = false;
            }
            else if (folderName == "Deleted Items")
            {
                userMessage.IsDeleted = true;
                userMessage.Folder = null;
            }
            else
            {
                userMessage.Folder = folderName;
                userMessage.IsDeleted = false;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task CopyMessageAsync(string messageId, string folderName, string userEmail)
    {
        // Copying a message in this architecture means creating a NEW MessageEntity?
        // Or just a NEW UserMessageEntity pointing to the same MessageEntity?
        // If we point to the same MessageEntity, it's efficient.
        // But if the user deletes one "copy", the other should remain.
        // Since UserMessageEntity tracks state, we can have multiple UserMessageEntities for the same user pointing to the same MessageEntity?
        // But (UserId, MessageId) might be unique?
        // Let's assume we want a full copy for now to be safe and independent.

        folderName = System.Net.WebUtility.UrlDecode(folderName);
        var originalMessage = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId);

        if (originalMessage != null)
        {
            var newMessageId = Guid.NewGuid().ToString();

            // Duplicate Message Content
            var newMessage = new MessageEntity
            {
                Id = newMessageId,
                From = originalMessage.From,
                To = originalMessage.To,
                Subject = originalMessage.Subject,
                Date = originalMessage.Date,
                RawContent = originalMessage.RawContent,
                IsImported = originalMessage.IsImported
            };
            _context.Messages.Add(newMessage);

            // Create User State
            var newUserMessage = new UserMessageEntity
            {
                UserId = userEmail,
                MessageId = newMessageId,
                Folder = (folderName == "Inbox" || folderName == "Deleted Items") ? null : folderName,
                IsRead = false, // Or copy read state?
                IsDeleted = (folderName == "Deleted Items")
            };
            _context.UserMessages.Add(newUserMessage);

            await _context.SaveChangesAsync();
        }
    }

    public async Task<(IEnumerable<MimeMessage> Messages, int TotalCount)> GetAllMessagesAsync(int page, int pageSize)
    {
        // Admin view? Just show all messages.
        var query = _context.Messages.Where(m => !m.IsImported).AsQueryable();
        var totalCount = await query.CountAsync();

        var entities = await query
            .OrderByDescending(m => m.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var messages = new List<MimeMessage>();
        foreach (var entity in entities)
        {
            using var stream = new MemoryStream(entity.RawContent);
            var message = await MimeMessage.LoadAsync(stream);
            message.Headers.Add("X-Db-Id", entity.Id);
            messages.Add(message);
        }

        return (messages, totalCount);
    }

    public async Task<(string Id, string From, string Subject)?> GetLatestMessageSummaryAsync(string userEmail, string folder = "Inbox")
    {
        var query = from um in _context.UserMessages
                    join m in _context.Messages on um.MessageId equals m.Id
                    where um.UserId == userEmail
                    select new { um, m };

        if (folder.Equals("Deleted Items", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => x.um.IsDeleted);
        }
        else if (folder.Equals("Inbox", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => !x.um.IsDeleted && x.um.Folder == null);
        }
        else
        {
            query = query.Where(x => !x.um.IsDeleted && x.um.Folder == folder);
        }

        var result = await query
            .OrderByDescending(x => x.m.Date)
            .Select(x => new { x.m.Id, x.m.From, x.m.Subject })
            .FirstOrDefaultAsync();

        if (result == null) return null;

        return (result.Id, result.From, result.Subject);
    }

    public async Task UpdateMessageAsync(string id, MimeMessage message)
    {
        var entity = await _context.Messages.FindAsync(id);
        if (entity != null)
        {
            using var memoryStream = new MemoryStream();
            await message.WriteToAsync(memoryStream);
            var rawContent = memoryStream.ToArray();

            entity.From = message.From.ToString();
            entity.To = message.To.ToString();
            entity.Subject = message.Subject ?? "(No Subject)";
            entity.Date = message.Date.DateTime;
            entity.RawContent = rawContent;

            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<string, int>> GetUnreadCountsAsync(string userEmail)
    {
        var unreadCounts = new Dictionary<string, int>();

        var unreadMessages = await _context.UserMessages
            .Where(um => um.UserId == userEmail && !um.IsRead)
            .Select(um => new { um.Folder, um.IsDeleted })
            .ToListAsync();

        // Inbox (Folder is null and not deleted)
        unreadCounts["Inbox"] = unreadMessages.Count(um => um.Folder == null && !um.IsDeleted);

        // Deleted Items (IsDeleted is true)
        unreadCounts["Deleted Items"] = unreadMessages.Count(um => um.IsDeleted);

        // Other folders
        var folderGroups = unreadMessages
            .Where(um => um.Folder != null && !um.IsDeleted)
            .GroupBy(um => um.Folder);

        foreach (var group in folderGroups)
        {
            if (group.Key != null)
            {
                unreadCounts[group.Key] = group.Count();
            }
        }

        return unreadCounts;
    }
}
