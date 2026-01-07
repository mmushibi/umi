using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Persistence;

namespace UmiHealth.Identity
{
    /// <summary>
    /// Implementation of token blacklist service
    /// Manages blacklisted tokens to prevent reuse after logout or revocation
    /// </summary>
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<TokenBlacklistService> _logger;

        public TokenBlacklistService(
            UmiHealthDbContext context,
            ILogger<TokenBlacklistService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
                
                if (jsonToken?.Id == null)
                {
                    return false;
                }

                var jti = jsonToken.Id;
                return await _context.BlacklistedTokens
                    .AnyAsync(t => t.TokenId == jti && 
                                  (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow), 
                              cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if token is blacklisted");
                return false;
            }
        }

        public async Task BlacklistTokenAsync(string token, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jsonToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
                
                if (jsonToken?.Id == null)
                {
                    _logger.LogWarning("Invalid token format for blacklisting");
                    return;
                }

                var jti = jsonToken.Id;
                var expiresAt = jsonToken.ValidTo;

                var blacklistedToken = new BlacklistedToken
                {
                    TokenId = jti,
                    Token = token,
                    Reason = reason,
                    BlacklistedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt
                };

                _context.BlacklistedTokens.Add(blacklistedToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Token {TokenId} blacklisted for reason: {Reason}", jti, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting token");
                throw;
            }
        }

        public async Task BlacklistUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get all active refresh tokens for the user and blacklist them
                var refreshTokens = await _context.RefreshTokens
                    .Where(t => t.UserId == userId && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                foreach (var refreshToken in refreshTokens)
                {
                    var blacklistedToken = new BlacklistedToken
                    {
                        TokenId = refreshToken.Id.ToString(),
                        Token = refreshToken.Token,
                        Reason = reason,
                        BlacklistedAt = DateTime.UtcNow,
                        ExpiresAt = refreshToken.ExpiresAt
                    };

                    _context.BlacklistedTokens.Add(blacklistedToken);
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("All tokens for user {UserId} blacklisted for reason: {Reason}", userId, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting user tokens");
                throw;
            }
        }

        public async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var expiredTokens = await _context.BlacklistedTokens
                    .Where(t => t.ExpiresAt != null && t.ExpiresAt <= DateTime.UtcNow)
                    .ToListAsync(cancellationToken);

                if (expiredTokens.Any())
                {
                    _context.BlacklistedTokens.RemoveRange(expiredTokens);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Cleaned up {Count} expired blacklisted tokens", expiredTokens.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
                throw;
            }
        }
    }
}
