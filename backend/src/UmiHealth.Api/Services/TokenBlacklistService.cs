using Microsoft.EntityFrameworkCore;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure;

namespace UmiHealth.Api.Services
{
    public interface ITokenBlacklistService
    {
        Task<bool> IsTokenBlacklistedAsync(string tokenId);
        Task BlacklistTokenAsync(string tokenId, DateTime expiresAt);
        Task BlacklistUserTokensAsync(Guid userId);
        Task CleanExpiredTokensAsync();
    }

    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<TokenBlacklistService> _logger;

        public TokenBlacklistService(UmiHealthDbContext context, ILogger<TokenBlacklistService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
                return false;

            var blacklisted = await _context.BlacklistedTokens
                .AnyAsync(bt => bt.TokenId == tokenId && bt.ExpiresAt > DateTime.UtcNow);

            return blacklisted;
        }

        public async Task BlacklistTokenAsync(string tokenId, DateTime expiresAt)
        {
            if (string.IsNullOrEmpty(tokenId))
                return;

            var blacklistedToken = new BlacklistedToken
            {
                TokenId = tokenId,
                BlacklistedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            _context.BlacklistedTokens.Add(blacklistedToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Token {TokenId} has been blacklisted", tokenId);
        }

        public async Task BlacklistUserTokensAsync(Guid userId)
        {
            // Get all active refresh tokens for the user
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsUsed)
                .ToListAsync();

            // Blacklist all refresh tokens
            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.IsRevoked = true;
                
                // Also blacklist the JWT token ID if available
                if (!string.IsNullOrEmpty(refreshToken.JwtTokenId))
                {
                    await BlacklistTokenAsync(refreshToken.JwtTokenId, refreshToken.ExpiresAt);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("All tokens for user {UserId} have been blacklisted", userId);
        }

        public async Task CleanExpiredTokensAsync()
        {
            var expiredTokens = await _context.BlacklistedTokens
                .Where(bt => bt.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync();

            if (expiredTokens.Any())
            {
                _context.BlacklistedTokens.RemoveRange(expiredTokens);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
            }
        }
    }
}
