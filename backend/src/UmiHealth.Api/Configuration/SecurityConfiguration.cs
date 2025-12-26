namespace UmiHealth.Api.Configuration
{
    /// <summary>
    /// Security configuration settings
    /// </summary>
    public class SecurityConfiguration
    {
        public const string DefaultPolicy = "Default";
        public const string AuthPolicy = "Auth";
        public const string ReadPolicy = "Read";
        public const string WritePolicy = "Write";
        public const string PremiumPolicy = "Premium";

        /// <summary>
        /// Rate limiting configuration
        /// </summary>
        public class RateLimiting
        {
            public int DefaultRequestsPerMinute { get; set; } = 100;
            public int AuthRequestsPerMinute { get; set; } = 10;
            public int ReadRequestsPerMinute { get; set; } = 200;
            public int WriteRequestsPerMinute { get; set; } = 50;
            public int PremiumRequestsPerMinute { get; set; } = 500;

            public int DefaultWindowSizeMinutes { get; set; } = 1;
            public int DefaultSegmentsPerWindow { get; set; } = 10;
            public int AuthSegmentsPerWindow { get; set; } = 2;
            public int WriteSegmentsPerWindow { get; set; } = 5;
            public int PremiumSegmentsPerWindow { get; set; } = 20;
        }

        /// <summary>
        /// CORS configuration
        /// </summary>
        public class Cors
        {
            public string[] AllowedOrigins { get; set; } = { "http://localhost:3000", "https://localhost:3000" };
            public string[] AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
            public string[] AllowedHeaders { get; set; } = { 
                "Content-Type", "Authorization", "X-Requested-With", "X-Version", "X-CSRF-Token" 
            };
            public int PreflightMaxAgeHours { get; set; } = 1;
            public bool AllowCredentials { get; set; } = true;
        }

        /// <summary>
        /// Content Security Policy configuration
        /// </summary>
        public class ContentSecurityPolicy
        {
            public string DefaultSrc { get; set; } = "'self'";
            public string ScriptSrc { get; set; } = "'self' 'unsafe-inline' 'unsafe-eval'";
            public string StyleSrc { get; set; } = "'self' 'unsafe-inline'";
            public string ImgSrc { get; set; } = "'self' data: https:";
            public string ConnectSrc { get; set; } = "'self' wss:";
            public string FontSrc { get; set; } = "'self' data:";
            public string FrameAncestors { get; set; } = "'none'";
            public string BaseUri { get; set; } = "'self'";
            public string FormAction { get; set; } = "'self'";
        }

        /// <summary>
        /// Input validation configuration
        /// </summary>
        public class InputValidation
        {
            public long MaxRequestSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
            public bool EnableXssProtection { get; set; } = true;
            public bool EnableSqlInjectionProtection { get; set; } = true;
            public string[] SkippedPaths { get; set; } = { 
                "/health", "/swagger", "/hangfire", "/upload", "/export" 
            };
        }

        /// <summary>
        /// HTTPS configuration
        /// </summary>
        public class Https
        {
            public bool RequireHttps { get; set; } = true;
            public bool EnableHsts { get; set; } = true;
            public int HstsMaxAgeDays { get; set; } = 365;
            public bool HstsIncludeSubDomains { get; set; } = true;
            public bool HstsPreload { get; set; } = true;
            public int HstsMaxAgeSeconds => HstsMaxAgeDays * 24 * 60 * 60;
        }

        /// <summary>
        /// Session security configuration
        /// </summary>
        public class Session
        {
            public int JwtExpirationMinutes { get; set; } = 60;
            public int RefreshTokenExpirationDays { get; set; } = 7;
            public bool RequireHttpsForTokens { get; set; } = true;
            public bool EnableTokenBlacklist { get; set; } = true;
            public int MaxFailedAttempts { get; set; } = 5;
            public int LockoutMinutes { get; set; } = 15;
        }

        /// <summary>
        /// Encryption configuration
        /// </summary>
        public class Encryption
        {
            public bool EnableDataEncryption { get; set; } = true;
            public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";
            public int KeySizeBits { get; set; } = 256;
            public int IvSizeBytes { get; set; } = 12;
            public int TagSizeBytes { get; set; } = 16;
        }
    }
}
