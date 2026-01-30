using UmiHealth.MinimalApi.Models;

namespace UmiHealth.MinimalApi.Services;

public interface IUserService
{
    Task<(bool Success, string Message, User? User)> AuthenticateUserAsync(string username, string password);
    Task<(bool Success, string Message, User? User)> CreateUserAsync(Dictionary<string, string> userData);
    Task<(bool Success, string Message)> UpdateUserAsync(string userId, Dictionary<string, string> updateData);
    Task<(bool Success, string Message)> DeleteUserAsync(string userId);
    Task<List<User>> GetUsersByTenantAsync(string tenantId);
    Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<(bool Success, string Message)> ResetPasswordAsync(string email);
    Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null);
    Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null);
    Task<(bool Success, string Message, User? User)> GetUserByIdAsync(string userId);
}
