using ExchangeMail.Core.Services;
using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Web.Controllers;
using ExchangeMail.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MimeKit;

namespace ExchangeMail.Tests.Controllers;

public class MailControllerTests
{
    private readonly Mock<IMailRepository> _mockRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IConfigurationService> _mockConfigService;
    private readonly Mock<ILogRepository> _mockLogRepo;
    private readonly Mock<ISafeSenderRepository> _mockSafeSenderRepo;
    private readonly Mock<IContactRepository> _mockContactRepo;
    private readonly Mock<IBlockListRepository> _mockBlockListRepo;
    private readonly HtmlSanitizerService _htmlSanitizerService;
    private readonly MailController _controller;

    public MailControllerTests()
    {
        _mockRepo = new Mock<IMailRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockConfigService = new Mock<IConfigurationService>();
        _mockLogRepo = new Mock<ILogRepository>();
        _mockSafeSenderRepo = new Mock<ISafeSenderRepository>();
        _mockContactRepo = new Mock<IContactRepository>();
        _mockBlockListRepo = new Mock<IBlockListRepository>();
        _htmlSanitizerService = new HtmlSanitizerService();

        _controller = new MailController(
            _mockRepo.Object,
            _mockUserRepo.Object,
            _mockConfigService.Object,
            _mockLogRepo.Object,
            _mockSafeSenderRepo.Object,
            _htmlSanitizerService,
            null!, // PstImportService
            null!, // ImportStatusService
            null!, // IServiceScopeFactory
            _mockContactRepo.Object,
            _mockBlockListRepo.Object
        );

        var session = new Mock<ISession>();
        var usernameBytes = System.Text.Encoding.UTF8.GetBytes("testuser");
        session.Setup(s => s.TryGetValue("Username", out usernameBytes)).Returns(true);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = session.Object;
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        _mockConfigService.Setup(c => c.GetDomainAsync()).ReturnsAsync("example.com");
    }

    [Fact]
    public async Task Index_ReturnsViewResult_WithMessages()
    {
        // Arrange
        _mockConfigService.Setup(c => c.GetDomainAsync()).ReturnsAsync("example.com");

        var messages = new List<MimeMessage>
        {
            new MimeMessage() { Subject = "Test 1" },
            new MimeMessage() { Subject = "Test 2" }
        };

        _mockRepo.Setup(r => r.GetMessagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((messages, 2));

        // Act
        var result = await _controller.Index(null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<MimeMessage>>(viewResult.ViewData.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Details_ReturnsViewResult_WhenMessageExists()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage() { Subject = "Test Details" };
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.Details(messageId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<MimeMessage>(viewResult.ViewData.Model);
        Assert.Equal("Test Details", model.Subject);
        _mockRepo.Verify(r => r.MarkAsReadAsync(messageId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenMessageDoesNotExist()
    {
        // Arrange
        var messageId = "999";
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync((MimeMessage?)null);

        // Act
        var result = await _controller.Details(messageId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_RedirectsToIndex_WhenMessageDeleted()
    {
        // Arrange
        var messageId = "123";
        _mockRepo.Setup(r => r.DeleteMessageAsync(messageId, It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(messageId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockRepo.Verify(r => r.DeleteMessageAsync(messageId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task MessagePartial_ReturnsPartialView_WhenMessageExists()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage() { Subject = "Test Partial" };
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.MessagePartial(messageId);

        // Assert
        var partialViewResult = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ReadingPane", partialViewResult.ViewName);
        var model = Assert.IsAssignableFrom<MimeMessage>(partialViewResult.ViewData.Model);
        Assert.Equal("Test Partial", model.Subject);
        _mockRepo.Verify(r => r.MarkAsReadAsync(messageId, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Download_ReturnsFileResult_WhenMessageExists()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("", "sender@example.com"));
        message.To.Add(new MailboxAddress("", "recipient@example.com"));
        message.Subject = "Test Download";
        message.Body = new TextPart("plain") { Text = "Content" };

        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.Download(messageId);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("message/rfc822", fileResult.ContentType);
        Assert.Equal($"{messageId}.eml", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task Compose_ReturnsViewResult()
    {
        // Arrange
        _mockConfigService.Setup(c => c.GetDomainAsync()).ReturnsAsync("example.com");

        // Act
        var result = await _controller.Compose("to@example.com", "Subject", "Body", null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("to@example.com", viewResult.ViewData["To"]);
        Assert.Equal("Subject", viewResult.ViewData["Subject"]);
        Assert.Equal("Body", viewResult.ViewData["Body"]);
    }

    [Fact]
    public async Task Body_ReturnsContentResult_WithSanitizedHtml()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage();
        message.Body = new TextPart("html") { Text = "<p>Test</p><script>alert('xss')</script>" };
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.Body(messageId);

        // Assert
        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html", contentResult.ContentType);
        Assert.Contains("<p>Test</p>", contentResult.Content);
        Assert.DoesNotContain("<script>", contentResult.Content);
    }

    [Fact]
    public async Task Body_ReturnsNotFound_WhenMessageDoesNotExist()
    {
        // Arrange
        var messageId = "999";
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync((MimeMessage?)null);

        // Act
        var result = await _controller.Body(messageId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Reply_ReturnsViewResult_WithPopulatedData()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.Subject = "Original Subject";
        message.Body = new TextPart("plain") { Text = "Original Body" };
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.Reply(messageId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Compose", redirectResult.ActionName);
        Assert.Equal("sender@example.com", redirectResult.RouteValues["to"]);
        Assert.Equal("Re: Original Subject", redirectResult.RouteValues["subject"]);
        Assert.Contains("> Original Body", (string)redirectResult.RouteValues["body"]);
    }

    [Fact]
    public async Task ReplyAll_ReturnsViewResult_WithPopulatedData()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.Cc.Add(new MailboxAddress("CC", "cc@example.com"));
        message.Subject = "Original Subject";
        message.Body = new TextPart("plain") { Text = "Original Body" };
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.ReplyAll(messageId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Compose", redirectResult.ActionName);
        Assert.Contains("sender@example.com", (string)redirectResult.RouteValues["to"]);
        Assert.Contains("cc@example.com", (string)redirectResult.RouteValues["to"]);
        Assert.Equal("Re: Original Subject", redirectResult.RouteValues["subject"]);
    }

    [Fact]
    public async Task Forward_ReturnsViewResult_WithPopulatedData()
    {
        // Arrange
        var messageId = "123";
        var message = new MimeMessage();
        message.Subject = "Original Subject";
        message.Body = new TextPart("plain") { Text = "Original Body" };
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.Forward(messageId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Compose", redirectResult.ActionName);
        Assert.Equal("Fwd: Original Subject", redirectResult.RouteValues["subject"]);
        Assert.Contains("---------- Forwarded message ----------", (string)redirectResult.RouteValues["body"]);
    }

    [Fact]
    public async Task MoveMessage_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        var messageId = "123";
        var folder = "Archive";
        _mockRepo.Setup(r => r.MoveMessageAsync(messageId, folder, It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.MoveMessage(messageId, folder);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockRepo.Verify(r => r.MoveMessageAsync(messageId, folder, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CopyMessage_RedirectsToIndex_WhenSuccessful()
    {
        // Arrange
        var messageId = "123";
        var folder = "Archive";
        _mockRepo.Setup(r => r.CopyMessageAsync(messageId, folder, It.IsAny<string>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CopyMessage(messageId, folder);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockRepo.Verify(r => r.CopyMessageAsync(messageId, folder, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task TrustSender_RedirectsToReturnUrl_WhenSuccessful()
    {
        // Arrange
        var sender = "sender@example.com";
        var returnUrl = "/Mail/Details/123";

        // Act
        var result = await _controller.TrustSender(sender, returnUrl);

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal(returnUrl, redirectResult.Url);
        _mockSafeSenderRepo.Verify(r => r.AddSafeSenderAsync(sender), Times.Once);
    }

    [Fact]
    public async Task EmptyTrash_RedirectsToIndex_WhenSuccessful()
    {
        // Act
        var result = await _controller.EmptyTrash();

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockRepo.Verify(r => r.EmptyTrashAsync("testuser@example.com"), Times.Once);
    }

    [Fact]
    public async Task SearchContacts_ReturnsJsonResult_WithMatchingContactsAndUsers()
    {
        // Arrange
        var query = "test";
        var contacts = new List<ContactEntity>
        {
            new ContactEntity { Name = "Test Contact", Email = "contact@test.com" }
        };
        var users = new List<UserEntity>
        {
            new UserEntity { Username = "testuser", Password = "password", IsAdmin = false }
        };

        _mockContactRepo.Setup(r => r.SearchContactsAsync(query)).ReturnsAsync(contacts);
        _mockUserRepo.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(users);
        _mockConfigService.Setup(c => c.GetDomainAsync()).ReturnsAsync("example.com");

        // Act
        var result = await _controller.SearchContacts(query);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var data = Assert.IsAssignableFrom<IEnumerable<object>>(jsonResult.Value);
        Assert.Equal(2, data.Count());
    }

    [Fact]
    public async Task MarkAsJunk_MovesMessageToJunkAndBlocksSender()
    {
        // Arrange
        var messageId = "123";
        var sender = "spammer@example.com";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Spammer", sender));
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.MarkAsJunk(messageId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockBlockListRepo.Verify(r => r.AddBlockedSenderAsync(sender), Times.Once);
        _mockSafeSenderRepo.Verify(r => r.RemoveSafeSenderAsync(sender), Times.Once);
        _mockRepo.Verify(r => r.MoveMessageAsync(messageId, "Junk Email", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsNotJunk_MovesMessageToInboxAndUnblocksSender()
    {
        // Arrange
        var messageId = "123";
        var sender = "friend@example.com";
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Friend", sender));
        _mockRepo.Setup(r => r.GetMessageAsync(messageId, It.IsAny<string>())).ReturnsAsync(message);

        // Act
        var result = await _controller.MarkAsNotJunk(messageId);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockBlockListRepo.Verify(r => r.RemoveBlockedSenderAsync(sender), Times.Once);
        _mockSafeSenderRepo.Verify(r => r.AddSafeSenderAsync(sender), Times.Once);
        _mockRepo.Verify(r => r.MoveMessageAsync(messageId, "Inbox", It.IsAny<string>()), Times.Once);
    }
}
