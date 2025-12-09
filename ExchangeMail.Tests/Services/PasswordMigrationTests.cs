using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using ExchangeMail.Core.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExchangeMail.Tests.Services;

public class PasswordMigrationTests
{
    private readonly ExchangeMailContext _context;
    private readonly SqliteUserRepository _repository;

    public PasswordMigrationTests()
    {
        var options = new DbContextOptionsBuilder<ExchangeMailContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ExchangeMailContext(options);
        _repository = new SqliteUserRepository(_context);
    }

    [Fact]
    public async Task CreateUserAsync_StoresHashedPassword()
    {
        // Act
        var username = "newuser";
        var password = "SafePassword123!";
        await _repository.CreateUserAsync(username, password, false);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        Assert.NotNull(user);
        Assert.NotEqual(password, user.Password);
        Assert.StartsWith("$2", user.Password); // BCrypt prefix
    }

    [Fact]
    public async Task ValidateUserAsync_MigratesLegacyPassword()
    {
        // Arrange: Create a user with a plain text password manually
        var username = "legacyuser";
        var plainPassword = "LegacyPassword123!";
        _context.Users.Add(new UserEntity
        {
            Username = username,
            Password = plainPassword,
            IsAdmin = false
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateUserAsync(username, plainPassword);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);

        // Verify it was updated in DB
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        Assert.NotEqual(plainPassword, userInDb.Password);
        Assert.StartsWith("$2", userInDb.Password);
    }

    [Fact]
    public async Task ValidateUserAsync_ValidatesHashedPassword()
    {
        // Arrange
        var username = "hasheduser";
        var password = "HashedPassword123!";
        // Use repo to create (result is hashed)
        await _repository.CreateUserAsync(username, password, false);

        // Act
        var result = await _repository.ValidateUserAsync(username, password);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(username, result.Username);
    }

    [Fact]
    public async Task ValidateUserAsync_RejectsInvalidPassword_ForLegacyUser()
    {
        // Arrange
        var username = "legacyuser_fail";
        var plainPassword = "CorrectPassword";
        _context.Users.Add(new UserEntity
        {
            Username = username,
            Password = plainPassword,
            IsAdmin = false
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ValidateUserAsync(username, "WrongPassword");

        // Assert
        Assert.Null(result);

        // Passwords should NOT be changed if login failed
        var userInDb = await _context.Users.FirstAsync(u => u.Username == username);
        Assert.Equal(plainPassword, userInDb.Password);
    }

    [Fact]
    public async Task ValidateUserAsync_RejectsInvalidPassword_ForHashedUser()
    {
        // Arrange
        var username = "hasheduser_fail";
        var password = "CorrectPassword";
        await _repository.CreateUserAsync(username, password, false);

        // Act
        var result = await _repository.ValidateUserAsync(username, "WrongPassword");

        // Assert
        Assert.Null(result);
    }
}
