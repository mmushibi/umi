using UmiHealth.Core.Entities;

namespace UmiHealth.Identity.Services;

public interface ITokenService
{
    Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, Guid tenantId, string jwtTokenId, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> RevokeAllUserRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
}
