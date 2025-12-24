namespace UmiHealth.Identity
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpiration { get; set; } = 15; // minutes
        public int RefreshTokenExpiration { get; set; } = 168; // hours (7 days)
    }
}
