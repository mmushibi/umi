namespace UmiHealth.Core.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task<bool> IsTokenBlacklistedAsync(string tokenId);
        Task BlacklistTokenAsync(string tokenId, DateTime expiresAt);
        Task BlacklistUserTokensAsync(Guid userId);
        Task CleanExpiredTokensAsync();
    }
}
