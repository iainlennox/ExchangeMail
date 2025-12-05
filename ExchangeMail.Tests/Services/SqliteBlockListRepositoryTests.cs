using ExchangeMail.Core.Data;
using ExchangeMail.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class SqliteBlockListRepositoryTests
{
    private readonly ExchangeMailContext _context;
    private readonly SqliteBlockListRepository _repository;

    public SqliteBlockListRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ExchangeMailContext(options);
        _repository = new SqliteBlockListRepository(_context);
    }

    [Fact]
    public async Task AddBlockedSenderAsync_AddsEmail()
    {
        // Act
        await _repository.AddBlockedSenderAsync("spam@example.com");

        // Assert
        Assert.True(await _repository.IsBlockedAsync("spam@example.com"));
        Assert.True(await _repository.IsBlockedAsync("SPAM@example.com")); // Case insensitive check
    }

    [Fact]
    public async Task AddBlockedSenderAsync_DoesNotAddDuplicate()
    {
        // Arrange
        await _repository.AddBlockedSenderAsync("spam@example.com");

        // Act
        await _repository.AddBlockedSenderAsync("spam@example.com");

        // Assert
        var count = await _context.BlockedSenders.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RemoveBlockedSenderAsync_RemovesEmail()
    {
        // Arrange
        await _repository.AddBlockedSenderAsync("spam@example.com");

        // Act
        await _repository.RemoveBlockedSenderAsync("spam@example.com");

        // Assert
        Assert.False(await _repository.IsBlockedAsync("spam@example.com"));
    }
}
