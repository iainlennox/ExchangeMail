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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        // 1. Try to verify as a BCrypt hash
        bool isValid = false;
        bool needsMigration = false;

        // Basic check to see if it looks like a BCrypt hash (starts with $2a$, $2b$, $2x$, $2y$)
        // If not, we assume it's legacy plain text
        if (IsBCryptHash(user.Password))
        {
            try
            {
                isValid = BCrypt.Net.BCrypt.Verify(password, user.Password);
            }
            catch (Exception)
            {
                // Verify failed (e.g. invalid salt), fall back to plain text check just in case
                // or simply fail. For safety, if it looked like a hash but failed, we fail.
                isValid = false;
            }
        }
        else
        {
            // Legacy Plain Text Check
            if (user.Password == password)
            {
                isValid = true;
                needsMigration = true;
            }
        }

        if (isValid)
        {
            if (needsMigration)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                await _context.SaveChangesAsync();
            }
            return user;
        }

        return null;
    }

    private bool IsBCryptHash(string password)
    {
        return !string.IsNullOrEmpty(password) &&
               (password.StartsWith("$2a$") ||
                password.StartsWith("$2b$") ||
                password.StartsWith("$2x$") ||
                password.StartsWith("$2y$")) &&
               password.Length == 60;
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
            Password = BCrypt.Net.BCrypt.HashPassword(password),
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

    public async Task UpdateAutoLabelingAsync(string username, bool enableAutoLabeling)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user != null)
        {
            user.EnableAutoLabeling = enableAutoLabeling;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> GetAutoLabelingAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user?.EnableAutoLabeling ?? false;
    }
}
