using System.Security.Claims;

namespace UmiHealth.Identity.Services;

public interface IJwtService
{
    string GenerateToken(ClaimsPrincipal claimsPrincipal);
    ClaimsPrincipal GetPrincipalFromToken(string token);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
}
