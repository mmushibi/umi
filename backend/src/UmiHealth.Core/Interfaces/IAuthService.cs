namespace UmiHealth.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<bool> LogoutAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfile> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid TenantId,
    Guid? BranchId
);

public record LoginRequest(
    string Email,
    string Password,
    string? TenantSubdomain
);

public record RefreshTokenRequest(
    string Token,
    string RefreshToken
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ForgotPasswordRequest(
    string Email,
    string? TenantSubdomain
);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword
);

public record AuthResponse(
    bool Success,
    string Message,
    UserDto? User,
    string? Token,
    string? RefreshToken,
    DateTime? ExpiresAt
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid TenantId,
    string TenantName,
    Guid? BranchId,
    string? BranchName,
    IList<string> Roles
);

public record UserProfile(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid TenantId,
    string TenantName,
    Guid? BranchId,
    string? BranchName,
    DateTime CreatedAt,
    DateTime LastLoginAt,
    bool IsActive
);
