using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Application.Services
{
    public interface IJwtTokenService
    {
        Task<string> GenerateAccessTokenAsync(User user);
        Task<string> GenerateRefreshTokenAsync(User user);
        Task<ClaimsPrincipal> ValidateAccessTokenAsync(string token);
        Task<ClaimsPrincipal> ValidateRefreshTokenAsync(string token);
        Task<bool> IsTokenExpiredAsync(string token);
        Task<RSA> GetPrivateKeyAsync();
        Task<RSA> GetPublicKeyAsync();
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly RSA _privateKey;
        private readonly RSA _publicKey;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            _privateKey = CreateOrLoadPrivateKey();
            _publicKey = CreateOrLoadPublicKey();
        }

        public async Task<string> GenerateAccessTokenAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new RsaSecurityKey(_privateKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("branch_id", user.BranchId?.ToString() ?? ""),
                new Claim("username", user.Username ?? user.Email),
                new Claim("permissions", System.Text.Json.JsonSerializer.Serialize(user.Permissions ?? new Dictionary<string, object>())),
                new Claim("branch_access", System.Text.Json.JsonSerializer.Serialize(user.BranchAccess ?? new List<Guid>()))
            };

            // Add role-based permissions
            var rolePermissions = GetRolePermissions(user.Role);
            foreach (var permission in rolePermissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15), // 15-minute expiry
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return await Task.FromResult(tokenHandler.WriteToken(token));
        }

        public async Task<string> GenerateRefreshTokenAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new RsaSecurityKey(_privateKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("token_type", "refresh")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7), // 7-day expiry
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return await Task.FromResult(tokenHandler.WriteToken(token));
        }

        public async Task<ClaimsPrincipal> ValidateAccessTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new RsaSecurityKey(_publicKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return await Task.FromResult(principal);
            }
            catch
            {
                return await Task.FromResult<ClaimsPrincipal>(null);
            }
        }

        public async Task<ClaimsPrincipal> ValidateRefreshTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new RsaSecurityKey(_publicKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false, // Refresh tokens don't require issuer validation
                    ValidateAudience = false, // Refresh tokens don't require audience validation
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Verify it's a refresh token
                var tokenType = principal.FindFirst("token_type")?.Value;
                if (tokenType != "refresh")
                {
                    return await Task.FromResult<ClaimsPrincipal>(null);
                }

                return await Task.FromResult(principal);
            }
            catch
            {
                return await Task.FromResult<ClaimsPrincipal>(null);
            }
        }

        public async Task<bool> IsTokenExpiredAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var jsonToken = tokenHandler.ReadJwtToken(token);
                return await Task.FromResult(jsonToken.ValidTo < DateTime.UtcNow);
            }
            catch
            {
                return await Task.FromResult(true);
            }
        }

        public async Task<RSA> GetPrivateKeyAsync()
        {
            return await Task.FromResult(_privateKey);
        }

        public async Task<RSA> GetPublicKeyAsync()
        {
            return await Task.FromResult(_publicKey);
        }

        private RSA CreateOrLoadPrivateKey()
        {
            var rsa = RSA.Create();
            
            // Try to load from configuration
            var privateKeyPem = _configuration["Jwt:PrivateKeyPem"];
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
            var rsa = RSA.Create();
            
            // Try to load from configuration
            var publicKeyPem = _configuration["Jwt:PublicKeyPem"];
            if (!string.IsNullOrEmpty(publicKeyPem))
            {
                try
                {
                    rsa.ImportFromPem(publicKeyPem);
                    return rsa;
                }
                catch
                {
                    // If loading fails, use the private key's public part
                }
            }

            // Use the public part of the private key
            var publicKey = _privateKey.ExportRSAPublicKey();
            rsa.ImportRSAPublicKey(publicKey, out _);
            
            return rsa;
        }

        private List<string> GetRolePermissions(string role)
        {
            return role.ToLower() switch
            {
                "super_admin" => new List<string>
                {
                    "system:*", "tenant:*", "user:*", "inventory:*", "reports:*", 
                    "pos:*", "prescriptions:*", "patients:*", "subscriptions:*", "branches:*"
                },
                "admin" => new List<string>
                {
                    "tenant:manage", "user:*", "inventory:*", "reports:*", 
                    "pos:*", "prescriptions:*", "patients:*", "branches:*"
                },
                "pharmacist" => new List<string>
                {
                    "patients:*", "prescriptions:*", "inventory:read", "inventory:write",
                    "reports:read", "pos:read"
                },
                "cashier" => new List<string>
                {
                    "pos:*", "patients:read", "inventory:read", "reports:sales"
                },
                "operations" => new List<string>
                {
                    "tenant:create", "subscriptions:*", "system:monitor", "reports:*"
                },
                _ => new List<string>()
            };
        }
    }
}
