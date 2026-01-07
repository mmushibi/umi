using Microsoft.EntityFrameworkCore;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Persistence;

namespace UmiHealth.Identity.Services;

public class TokenService : ITokenService
{
    private readonly ITenantRepository<RefreshToken> _refreshTokenRepository;
    private readonly ICacheService _cacheService;

    public TokenService(
        ITenantRepository<RefreshToken> refreshTokenRepository,
        ICacheService cacheService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _cacheService = cacheService;
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(
        Guid userId, 
        Guid tenantId, 
        string jwtTokenId, 
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString(),
            JwtTokenId = jwtTokenId,
            IsUsed = false,
            IsRevoked = false,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdToken = await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        // Cache the refresh token for quick lookup
        await _cacheService.SetAsync(
            $"refresh_token:{createdToken.Token}",
            createdToken,
            TimeSpan.FromDays(7),
            cancellationToken);

        return createdToken;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(
        string token, 
        CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cachedToken = await _cacheService.GetAsync<RefreshToken>(
            $"refresh_token:{token}", 
            cancellationToken);

        if (cachedToken != null)
            return cachedToken;

        // Fallback to database
        var dbToken = (await _refreshTokenRepository.FindAsync(
            rt => rt.Token == token && !rt.IsUsed && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, 
            cancellationToken)).FirstOrDefault();

        if (dbToken != null)
        {
            // Cache for future requests
            await _cacheService.SetAsync(
                $"refresh_token:{token}",
                dbToken,
                TimeSpan.FromDays(7),
                cancellationToken);
        }

        return dbToken;
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        string token, 
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetRefreshTokenAsync(token, cancellationToken);
        if (refreshToken == null)
            return false;

        refreshToken.IsRevoked = true;
        refreshToken.UpdatedAt = DateTime.UtcNow;

        await _refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        // Remove from cache
        await _cacheService.RemoveAsync($"refresh_token:{token}", cancellationToken);

        return true;
    }

    public async Task<bool> RevokeAllUserRefreshTokensAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var userTokens = await _refreshTokenRepository.FindAsync(
            rt => rt.UserId == userId && !rt.IsUsed && !rt.IsRevoked, 
            cancellationToken);

        foreach (var token in userTokens)
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTime.UtcNow;

            // Remove from cache
            await _cacheService.RemoveAsync($"refresh_token:{token.Token}", cancellationToken);
        }

        await _refreshTokenRepository.UpdateRangeAsync(userTokens, cancellationToken);
        return true;
    }

    public async Task<bool> ValidateRefreshTokenAsync(
        string token, 
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await GetRefreshTokenAsync(token, cancellationToken);
        
        return refreshToken != null && 
               !refreshToken.IsUsed && 
               !refreshToken.IsRevoked && 
               refreshToken.ExpiresAt > DateTime.UtcNow;
    }
}
