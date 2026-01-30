using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace UmiHealth.MinimalApi.Services;

public class UserService : IUserService
{
    private readonly UmiHealthDbContext _context;
    private readonly IValidationService _validationService;
    private readonly IAuditService _auditService;

    public UserService(UmiHealthDbContext context, IValidationService validationService, IAuditService auditService)
    {
        _context = context;
        _validationService = validationService;
        _auditService = auditService;
    }

    public async Task<(bool Success, string Message, User? User)> AuthenticateUserAsync(string username, string password)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username || u.Email == username);

            if (user == null)
            {
                return (false, "User not found", null);
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
            {
                return (false, "Invalid password", null);
            }

            if (!user.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Account is deactivated", null);
            }

            user.CreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Authentication successful", user);
        }
        catch (Exception ex)
        {
            return (false, $"Authentication error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message, User? User)> CreateUserAsync(Dictionary<string, string> userData)
    {
        try
        {
            var validationErrors = _validationService.ValidateRegistrationInput(userData);
            if (validationErrors.Any())
            {
                return (false, $"Validation failed: {string.Join(", ", validationErrors.Values)}", null);
            }

            var username = _validationService.SanitizeInput(userData["username"]);
            var email = _validationService.SanitizeInput(userData["email"]);

            if (!await IsUsernameAvailableAsync(username))
            {
                return (false, "Username is already taken", null);
            }

            if (!await IsEmailAvailableAsync(email))
            {
                return (false, "Email is already registered", null);
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(userData["password"]),
                FirstName = _validationService.SanitizeInput(userData["firstName"]),
                LastName = _validationService.SanitizeInput(userData["lastName"]),
                PhoneNumber = _validationService.SanitizeInput(userData["phoneNumber"] ?? ""),
                Role = "Admin",
                TenantId = Guid.NewGuid().ToString(),
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _auditService.LogAuthenticationEvent(
                user.Id, 
                user.TenantId, 
                "USER_CREATED", 
                null,
                true,
                $"Role: {user.Role}, Email: {user.Email}"
            );

            return (true, "User created successfully", user);
        }
        catch (Exception ex)
        {
            return (false, $"User creation error: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(string userId, Dictionary<string, string> updateData)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            if (updateData.TryGetValue("firstName", out var firstName))
            {
                user.FirstName = _validationService.SanitizeInput(firstName);
            }

            if (updateData.TryGetValue("lastName", out var lastName))
            {
                user.LastName = _validationService.SanitizeInput(lastName);
            }

            if (updateData.TryGetValue("phoneNumber", out var phoneNumber))
            {
                user.PhoneNumber = _validationService.SanitizeInput(phoneNumber);
            }

            if (updateData.TryGetValue("email", out var email))
            {
                if (!_validationService.IsValidEmail(email))
                {
                    return (false, "Invalid email format");
                }

                if (!await IsEmailAvailableAsync(email, userId))
                {
                    return (false, "Email is already in use");
                }

                user.Email = _validationService.SanitizeInput(email);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "User updated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"User update error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            user.Status = "deactivated";
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "User deactivated successfully");
        }
        catch (Exception ex)
        {
            return (false, $"User deletion error: {ex.Message}");
        }
    }

    public async Task<List<User>> GetUsersByTenantAsync(string tenantId)
    {
        return await _context.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
            {
                return (false, "Current password is incorrect");
            }

            if (!_validationService.IsValidPassword(newPassword))
            {
                return (false, "New password does not meet requirements");
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Password change error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return (false, "Email not found");
            }

            var tempPassword = GenerateTemporaryPassword();
            user.Password = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // In production, send email with tempPassword
            return (true, $"Password reset. Temporary password: {tempPassword}");
        }
        catch (Exception ex)
        {
            return (false, $"Password reset error: {ex.Message}");
        }
    }

    public async Task<bool> IsUsernameAvailableAsync(string username, string? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Username == username);
        if (!string.IsNullOrEmpty(excludeUserId))
        {
            query = query.Where(u => u.Id != excludeUserId);
        }
        return !await query.AnyAsync();
    }

    public async Task<bool> IsEmailAvailableAsync(string email, string? excludeUserId = null)
    {
        var query = _context.Users.Where(u => u.Email == email);
        if (!string.IsNullOrEmpty(excludeUserId))
        {
            query = query.Where(u => u.Id != excludeUserId);
        }
        return !await query.AnyAsync();
    }

    public async Task<(bool Success, string Message, User? User)> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return (false, "User not found", null);
            }

            return (true, "User found", user);
        }
        catch (Exception ex)
        {
            return (false, $"Error retrieving user: {ex.Message}", null);
        }
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new char[12];

        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }

        return new string(password);
    }
}
