using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure;
using UmiHealth.Core.Interfaces;

namespace UmiHealth.Identity
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<JwtService> _logger;
        private readonly RSA _privateKey;
        private readonly RSA _publicKey;
        private readonly ITokenBlacklistService _tokenBlacklistService;

        public JwtService(
            JwtSettings jwtSettings,
            UmiHealthDbContext context,
            ILogger<JwtService> logger,
            ITokenBlacklistService tokenBlacklistService)
        {
            _jwtSettings = jwtSettings;
            _context = context;
            _logger = logger;
            _tokenBlacklistService = tokenBlacklistService;
            _privateKey = CreateOrLoadPrivateKey();
            _publicKey = CreateOrLoadPublicKey();
        }

        public string GenerateAccessToken(User user, IEnumerable<Role> roles, Guid? branchId = null)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new RsaSecurityKey(_privateKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.GivenName, user.FirstName),
                new(ClaimTypes.Surname, user.LastName),
                new("tenant_id", user.TenantId.ToString()),
                new("user_name", user.UserName),
                new("phone_number", user.PhoneNumber),
                new("branch_id", branchId?.ToString() ?? string.Empty)
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
                claims.Add(new Claim("role_id", role.Id.ToString()));
            }

            // Add permission claims from roles
            var allPermissions = new List<string>();
            foreach (var role in roles)
            {
                var roleWithClaims = _context.Roles
                    .Include(r => r.RoleClaims)
                    .FirstOrDefault(r => r.Id == role.Id);

                if (roleWithClaims?.RoleClaims != null)
                {
                    allPermissions.AddRange(roleWithClaims.RoleClaims.Select(rc => $"{rc.ClaimType}:{rc.ClaimValue}"));
                }
            }

            // Add unique permissions as claims
            foreach (var permission in allPermissions.Distinct())
            {
                claims.Add(new Claim("permission", permission));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiration),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            var tokenId = token.Id;

            _logger.LogInformation("Generated access token {TokenId} for user {UserId} in tenant {TenantId}", tokenId, user.Id, user.TenantId);

            return tokenString;
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked && !rt.IsUsed, cancellationToken);

            return token != null && token.ExpiresAt > DateTime.UtcNow;
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

            if (token != null)
            {
                token.IsRevoked = true;
                
                // Blacklist the associated JWT token if it exists
                if (!string.IsNullOrEmpty(token.JwtTokenId))
                {
                    await _tokenBlacklistService.BlacklistTokenAsync(token.JwtTokenId, token.ExpiresAt);
                }
                
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Revoked refresh token {TokenId}", token.Id);
            }
        }

        public async Task SaveRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            // Revoke existing refresh tokens for the user
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == refreshToken.UserId && !rt.IsRevoked && !rt.IsUsed)
                .ToListAsync(cancellationToken);

            foreach (var existingToken in existingTokens)
            {
                existingToken.IsRevoked = true;
                
                // Blacklist the associated JWT token if it exists
                if (!string.IsNullOrEmpty(existingToken.JwtTokenId))
                {
                    await _tokenBlacklistService.BlacklistTokenAsync(existingToken.JwtTokenId, existingToken.ExpiresAt);
                }
            }

            // Add new refresh token
            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved refresh token {TokenId} for user {UserId}", refreshToken.Id, refreshToken.UserId);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(_publicKey),
                ValidateLifetime = false // We're validating an expired token
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.RsaSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

        private RSA CreateOrLoadPrivateKey()
        {
            var rsa = System.Security.Cryptography.RSA.Create();
            
            // Try to load from configuration
            var privateKeyPem = _jwtSettings.Secret;
            if (!string.IsNullOrEmpty(privateKeyPem))
            {
                try
                {
                    rsa.ImportFromPem(privateKeyPem);
                    return rsa;
                }
                catch
                {
                    // If loading fails, create new key
                }
            }

            // Generate new key pair
            rsa.KeySize = 2048;
            
            // Export the public key for storage
            var publicKeyPem = rsa.ExportRSAPublicKeyPem();
            var privateKeyPemGenerated = rsa.ExportRSAPrivateKeyPem();
            
            // In a production environment, you would store these securely
            // For now, we'll use the generated key in memory
            
            return rsa;
        }

        private RSA CreateOrLoadPublicKey()
        {
            var rsa = System.Security.Cryptography.RSA.Create();
            
            // Use the public part of the private key
            var publicKey = _privateKey.ExportRSAPublicKey();
            rsa.ImportRSAPublicKey(publicKey, out _);
            
            return rsa;
        }
    }
}
