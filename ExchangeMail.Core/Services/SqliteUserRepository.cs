using ExchangeMail.Core.Data;
using ExchangeMail.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMail.Core.Services;

public class SqliteUserRepository : IUserRepository
{
    private readonly ExchangeMailContext _context;

    public SqliteUserRepository(ExchangeMailContext context)
    {
        _context = context;
    }

    public async Task<bool> AnyUsersAsync()
    {
        return await _context.Users.AnyAsync();
    }

    public async Task<UserEntity?> ValidateUserAsync(string username, string password)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
    }

    public async Task CreateUserAsync(string username, string password, bool isAdmin)
    {
        if (await _context.Users.AnyAsync(u => u.Username == username))
        {
            throw new InvalidOperationException("User already exists");
        }

        _context.Users.Add(new UserEntity
        {
            Username = username,
            Password = password,
            IsAdmin = isAdmin
        });
        await _context.SaveChangesAsync();
    }

    public async Task<UserEntity?> GetUserAsync(string username)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<UserEntity?> AuthenticateAsync(string username, string password)
    {
        return await ValidateUserAsync(username, password);
    }

    public async Task<IEnumerable<UserEntity>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task DeleteUserAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }


    public async Task UpdateSignatureAsync(string username, string signature)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user != null)
        {
            user.Signature = signature;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<string?> GetSignatureAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user?.Signature;
    }

    public async Task UpdateAnimationsAsync(string username, bool enableAnimations)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user != null)
        {
            user.EnableAnimations = enableAnimations;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> GetAnimationsAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user?.EnableAnimations ?? false;
    }
}
