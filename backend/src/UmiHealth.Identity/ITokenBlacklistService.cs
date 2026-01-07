using System;
using System.Threading;
using System.Threading.Tasks;

namespace UmiHealth.Identity
{
    /// <summary>
    /// Service for managing token blacklist
    /// Prevents use of invalidated tokens (logout, force logout, etc.)
    /// </summary>
    public interface ITokenBlacklistService
    {
        /// <summary>
        /// Check if a token is blacklisted
        /// </summary>
        Task<bool> IsTokenBlacklistedAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a token to the blacklist
        /// </summary>
        Task BlacklistTokenAsync(string token, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Blacklist all tokens for a user
        /// </summary>
        Task BlacklistUserTokensAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clean up expired blacklisted tokens
        /// </summary>
        Task CleanupExpiredTokensAsync(CancellationToken cancellationToken = default);
    }
}
