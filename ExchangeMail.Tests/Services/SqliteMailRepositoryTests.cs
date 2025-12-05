using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Core.Services;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Moq;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class SqliteMailRepositoryTests
{
    private readonly ExchangeMailContext _context;
    private readonly Mock<ILogRepository> _mockLogRepo;
    private readonly SqliteMailRepository _repository;

    public SqliteMailRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        _context = new ExchangeMailContext(options);
        _mockLogRepo = new Mock<ILogRepository>();
        _repository = new SqliteMailRepository(_context, _mockLogRepo.Object);

        // Seed a user
        _context.Users.Add(new UserEntity { Username = "user@example.com", Password = "password" });
        _context.SaveChanges();
    }

    [Fact]
    public async Task SaveMessageAsync_AddsMessageToDatabase()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Receiver", "user@example.com"));
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "Test Body" };

        // Act
        await _repository.SaveMessageAsync(message);

        // Assert
        var entity = await _context.Messages.FirstOrDefaultAsync();
        Assert.NotNull(entity);
        Assert.Equal("Test Subject", entity.Subject);

        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == entity.Id);
        Assert.NotNull(userMessage);
        Assert.False(userMessage.IsRead);
    }

    [Fact]
    public async Task GetMessagesAsync_FiltersByUserAndSearch()
    {
        // Arrange
        var userEmail = "user@example.com";

        // Message 1: For user, matches search
        await AddMessageAsync("1", "sender@example.com", userEmail, "Important Update");

        // Message 2: For user, does NOT match search
        await AddMessageAsync("2", "sender@example.com", userEmail, "Lunch Plans");

        // Message 3: NOT for user, matches search
        await AddMessageAsync("3", "sender@example.com", "other@example.com", "Important Update");

        // Act
        var (messages, count) = await _repository.GetMessagesAsync(userEmail, "Important", 1, 10, "Inbox");

        // Assert
        Assert.Single(messages);
        Assert.Equal("Important Update", messages.First().Subject);
    }

    [Fact]
    public async Task MarkAsReadAsync_UpdatesIsReadFlag()
    {
        // Arrange
        var id = "123";
        await AddMessageAsync(id, "sender@example.com", "user@example.com", "Test");

        // Act
        await _repository.MarkAsReadAsync(id, "user@example.com");

        // Assert
        var entity = await _context.Messages.FindAsync(id);
        Assert.NotNull(entity);

        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == "user@example.com");
        Assert.NotNull(userMessage);
        Assert.True(userMessage.IsRead);
    }

    [Fact]
    public async Task DeleteMessageAsync_RemovesMessageFromDatabase()
    {
        // Arrange
        var id = "delete-test";
        await AddMessageAsync(id, "sender@example.com", "user@example.com", "To Delete");

        // Act
        await _repository.DeleteMessageAsync(id, "user@example.com");

        // Assert
        var entity = await _context.Messages.FindAsync(id);
        Assert.NotNull(entity);

        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == "user@example.com");
        Assert.NotNull(userMessage);
        Assert.True(userMessage.IsDeleted);
    }

    [Fact]
    public async Task GetMessageAsync_ReturnsMessage_WhenExists()
    {
        // Arrange
        var id = "get-test";
        await AddMessageAsync(id, "sender@example.com", "user@example.com", "To Get");

        // Act
        var message = await _repository.GetMessageAsync(id, "user@example.com");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("To Get", message.Subject);
    }

    [Fact]
    public async Task MoveMessageAsync_UpdatesFolder()
    {
        // Arrange
        var id = "move-test";
        await AddMessageAsync(id, "sender@example.com", "user@example.com", "To Move");
        var folder = "Archive";

        // Act
        await _repository.MoveMessageAsync(id, folder, "user@example.com");

        // Assert
        var entity = await _context.Messages.FindAsync(id);
        Assert.NotNull(entity);

        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == "user@example.com");
        Assert.NotNull(userMessage);
        Assert.Equal(folder, userMessage.Folder);
    }

    [Fact]
    public async Task CopyMessageAsync_CreatesNewMessageInFolder()
    {
        // Arrange
        var id = "copy-test";
        await AddMessageAsync(id, "sender@example.com", "user@example.com", "To Copy");
        var folder = "Archive";

        // Act
        await _repository.CopyMessageAsync(id, folder, "user@example.com");

        // Assert
        var original = await _context.Messages.FindAsync(id);
        Assert.NotNull(original);

        var originalUserMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id && um.UserId == "user@example.com");
        Assert.NotNull(originalUserMessage);
        Assert.NotEqual(folder, originalUserMessage.Folder); // Original should not change

        var copy = await _context.Messages.FirstOrDefaultAsync(m => m.Subject == "To Copy" && m.Id != id);
        Assert.NotNull(copy);

        var copyUserMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == copy.Id && um.UserId == "user@example.com");
        Assert.NotNull(copyUserMessage);
        Assert.Equal(folder, copyUserMessage.Folder);
    }

    [Fact]
    public async Task DeleteFolderAsync_RemovesFolderAndMovesMessagesToTrash()
    {
        // Arrange
        var userEmail = "user@example.com";
        var folderName = "TestFolder";
        await _repository.CreateFolderAsync(folderName, userEmail);

        var messageId = "msg-in-folder";
        await AddMessageAsync(messageId, "sender@example.com", userEmail, "Subject", folderName);

        // Act
        await _repository.DeleteFolderAsync(folderName, userEmail);

        // Assert
        var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Name == folderName);
        Assert.Null(folder);

        var message = await _context.Messages.FindAsync(messageId);
        Assert.NotNull(message);

        var userMessage = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == messageId && um.UserId == userEmail);
        Assert.NotNull(userMessage);
        Assert.Null(userMessage.Folder);
        Assert.True(userMessage.IsDeleted);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_UpdatesAllUnreadMessagesInFolder()
    {
        // Arrange
        var userEmail = "user@example.com";
        var folderName = "Inbox";

        // Message 1: Unread in Inbox
        var id1 = "msg1";
        await AddMessageAsync(id1, "sender@example.com", userEmail, "Subject 1"); // Default is Inbox (null folder)

        // Message 2: Read in Inbox
        var id2 = "msg2";
        await AddMessageAsync(id2, "sender@example.com", userEmail, "Subject 2");
        await _repository.MarkAsReadAsync(id2, userEmail);

        // Message 3: Unread in Other Folder
        var id3 = "msg3";
        await _repository.CreateFolderAsync("Other", userEmail);
        await AddMessageAsync(id3, "sender@example.com", userEmail, "Subject 3", "Other");

        // Act
        await _repository.MarkAllAsReadAsync(userEmail, folderName);

        // Assert
        var msg1 = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id1 && um.UserId == userEmail);
        Assert.True(msg1.IsRead);

        var msg2 = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id2 && um.UserId == userEmail);
        Assert.True(msg2.IsRead); // Should remain read

        var msg3 = await _context.UserMessages.FirstOrDefaultAsync(um => um.MessageId == id3 && um.UserId == userEmail);
        Assert.False(msg3.IsRead); // Should remain unread (different folder)
    }

    private async Task AddMessageAsync(string id, string from, string to, string subject, string? folder = null)
    {
        // Create a minimal valid MIME message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("", from));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = "" };

        using var stream = new MemoryStream();
        await message.WriteToAsync(stream);

        _context.Messages.Add(new MessageEntity
        {
            Id = id,
            From = from,
            To = to,
            Subject = subject,
            Date = DateTime.UtcNow,
            RawContent = stream.ToArray(),
            IsImported = false
        });

        _context.UserMessages.Add(new UserMessageEntity
        {
            UserId = to, // Assume 'to' is the user for this test context
            MessageId = id,
            Folder = folder,
            IsRead = false,
            IsDeleted = false
        });

        await _context.SaveChangesAsync();
    }
}
