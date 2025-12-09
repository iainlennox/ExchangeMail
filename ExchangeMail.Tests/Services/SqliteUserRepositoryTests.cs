using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class SqliteUserRepositoryTests
{
    private readonly ExchangeMailContext _context;
    private readonly SqliteUserRepository _repository;

    public SqliteUserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ExchangeMailContext(options);
        _repository = new SqliteUserRepository(_context);
    }

    [Fact]
    public async Task ValidateUserAsync_ReturnsUser_WhenCredentialsMatch()
    {
        // Arrange
        var username = "testuser";
        var password = "password";
        await _repository.CreateUserAsync(username, password, false);

        // Act
        var user = await _repository.ValidateUserAsync(username, password);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(username, user.Username);
    }

    [Fact]
    public async Task ValidateUserAsync_ReturnsNull_WhenCredentialsDoNotMatch()
    {
        // Arrange
        var username = "testuser";
        var password = "password";
        await _repository.CreateUserAsync(username, password, false);

        // Act
        var user = await _repository.ValidateUserAsync(username, "wrongpassword");

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public async Task CreateUserAsync_AddsUserToDatabase()
    {
        // Arrange
        var username = "newuser";
        var password = "password";

        // Act
        await _repository.CreateUserAsync(username, password, true);

        // Assert
        var entity = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        Assert.NotNull(entity);
        Assert.NotEqual(password, entity.Password);
        Assert.StartsWith("$2", entity.Password);
        Assert.True(entity.IsAdmin);
    }

    [Fact]
    public async Task CreateUserAsync_ThrowsException_WhenUserAlreadyExists()
    {
        // Arrange
        var username = "existinguser";
        var password = "password";
        await _repository.CreateUserAsync(username, password, false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.CreateUserAsync(username, "newpassword", false));
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers()
    {
        // Arrange
        await _repository.CreateUserAsync("user1", "pass", false);
        await _repository.CreateUserAsync("user2", "pass", false);

        // Act
        var users = await _repository.GetAllUsersAsync();

        // Assert
        Assert.Contains(users, u => u.Username == "user1");
        Assert.Contains(users, u => u.Username == "user2");
    }

    [Fact]
    public async Task AnyUsersAsync_ReturnsTrue_WhenUsersExist()
    {
        // Arrange
        await _repository.CreateUserAsync("user1", "pass", false);

        // Act
        var result = await _repository.AnyUsersAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AnyUsersAsync_ReturnsFalse_WhenNoUsersExist()
    {
        // Act
        var result = await _repository.AnyUsersAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteUserAsync_RemovesUserFromDatabase()
    {
        // Arrange
        var username = "userToDelete";
        await _repository.CreateUserAsync(username, "password", false);

        // Act
        await _repository.DeleteUserAsync(username);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        Assert.Null(user);
    }
}
