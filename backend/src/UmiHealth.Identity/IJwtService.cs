using UmiHealth.Core.Entities;

namespace UmiHealth.Identity
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user, IEnumerable<Role> roles, Guid? branchId = null);
        string GenerateRefreshToken();
        Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task SaveRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
