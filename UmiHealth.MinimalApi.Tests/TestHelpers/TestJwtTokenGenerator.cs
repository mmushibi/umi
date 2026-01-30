using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UmiHealth.MinimalApi.Tests.TestHelpers
{
    public static class TestJwtTokenGenerator
    {
        private const string SecretKey = "0)\"                                  DS![59;xM4yR|G3_%9E^U*}sK96k+I&$)98Qm\nO!iS@8D1NN%XszW^vD%II[ZWuX#";
        private const string Issuer = "UmiHealth";
        private const string Audience = "UmiHealthUsers";

        public static string GenerateToken(string userId, string email, string role, string tenantId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("tenant_id", tenantId),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                Issuer = Issuer,
                Audience = Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static string GenerateAdminToken()
        {
            return GenerateToken("user-1", "admin@test.com", "admin", "test-tenant-1");
        }

        public static string GenerateCashierToken()
        {
            return GenerateToken("user-2", "cashier@test.com", "cashier", "test-tenant-1");
        }

        public static string GenerateSuperAdminToken()
        {
            return GenerateToken("super-admin", "superadmin@test.com", "superadmin", "system");
        }
    }
}
