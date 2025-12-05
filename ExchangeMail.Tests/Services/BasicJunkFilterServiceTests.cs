using ExchangeMail.Core.Services;
using Moq;
using MimeKit;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class BasicJunkFilterServiceTests
{
    private readonly Mock<ISafeSenderRepository> _mockSafeSenderRepository;
    private readonly Mock<IBlockListRepository> _mockBlockListRepository;
    private readonly BasicJunkFilterService _service;

    public BasicJunkFilterServiceTests()
    {
        _mockSafeSenderRepository = new Mock<ISafeSenderRepository>();
        _mockBlockListRepository = new Mock<IBlockListRepository>();
        _service = new BasicJunkFilterService(_mockSafeSenderRepository.Object, _mockBlockListRepository.Object);
    }

    [Fact]
    public async Task IsJunkAsync_ReturnsFalse_WhenSenderIsSafe()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Safe Sender", "safe@example.com"));
        _mockSafeSenderRepository.Setup(r => r.IsSafeSenderAsync("safe@example.com")).ReturnsAsync(true);

        // Act
        var result = await _service.IsJunkAsync(message);

        // Assert
        Assert.False(result);
        _mockBlockListRepository.Verify(r => r.IsBlockedAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IsJunkAsync_ReturnsTrue_WhenSenderIsBlocked()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Spammer", "spam@example.com"));
        _mockSafeSenderRepository.Setup(r => r.IsSafeSenderAsync("spam@example.com")).ReturnsAsync(false);
        _mockBlockListRepository.Setup(r => r.IsBlockedAsync("spam@example.com")).ReturnsAsync(true);

        // Act
        var result = await _service.IsJunkAsync(message);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsJunkAsync_ReturnsFalse_WhenSenderIsNeitherSafeNorBlocked()
    {
        // Arrange
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Unknown", "unknown@example.com"));
        _mockSafeSenderRepository.Setup(r => r.IsSafeSenderAsync("unknown@example.com")).ReturnsAsync(false);
        _mockBlockListRepository.Setup(r => r.IsBlockedAsync("unknown@example.com")).ReturnsAsync(false);

        // Act
        var result = await _service.IsJunkAsync(message);

        // Assert
        Assert.False(result);
    }
}
