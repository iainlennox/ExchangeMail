using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class SqliteConfigurationServiceTests
{
    private readonly ExchangeMailContext _context;
    private readonly SqliteConfigurationService _service;

    public SqliteConfigurationServiceTests()
    {
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ExchangeMailContext(options);
        _service = new SqliteConfigurationService(_context);
    }

    [Fact]
    public async Task GetDomainAsync_ReturnsLocalhost_WhenNotSet()
    {
        // Act
        var domain = await _service.GetDomainAsync();

        // Assert
        Assert.Equal("localhost", domain);
    }

    [Fact]
    public async Task GetDomainAsync_ReturnsValue_WhenSet()
    {
        // Arrange
        _context.Configurations.Add(new ConfigEntity { Key = "Domain", Value = "example.com" });
        await _context.SaveChangesAsync();

        // Act
        var domain = await _service.GetDomainAsync();

        // Assert
        Assert.Equal("example.com", domain);
    }

    [Fact]
    public async Task SetDomainAsync_UpdatesValue()
    {
        // Arrange
        await _service.SetDomainAsync("initial.com");

        // Act
        await _service.SetDomainAsync("updated.com");

        // Assert
        var config = await _context.Configurations.FindAsync("Domain");
        Assert.NotNull(config);
        Assert.Equal("updated.com", config.Value);
    }
}
