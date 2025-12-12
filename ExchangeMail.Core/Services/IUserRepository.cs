using ExchangeMail.Core.Data.Entities;

namespace ExchangeMail.Core.Services;

public interface IUserRepository
{
    Task<UserEntity?> ValidateUserAsync(string username, string password);
    Task CreateUserAsync(string username, string password, bool isAdmin);
    Task<UserEntity?> GetUserAsync(string username);
    Task<UserEntity?> AuthenticateAsync(string username, string password);
    Task<IEnumerable<UserEntity>> GetAllUsersAsync();
    Task DeleteUserAsync(string username);
    Task<bool> AnyUsersAsync();
    Task UpdateSignatureAsync(string username, string signature);
    Task<string?> GetSignatureAsync(string username);
    Task UpdateAnimationsAsync(string username, bool enableAnimations);
    Task<bool> GetAnimationsAsync(string username);
    Task UpdateAutoLabelingAsync(string username, bool enableAutoLabeling);
    Task<bool> GetAutoLabelingAsync(string username);
    Task ResetPasswordAsync(string username, string newPassword);

    // 2FA
    Task SetTwoFactorSecretAsync(string username, string secret);
    Task<string?> GetTwoFactorSecretAsync(string username);
    Task SetTwoFactorEnabledAsync(string username, bool enabled);
    Task<bool> GetTwoFactorEnabledAsync(string username);
}
