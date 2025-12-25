using Xunit;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using UmiHealth.Identity.Services;

namespace UmiHealth.Tests.Unit.Identity.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", "ThisIsASecretKeyForTesting12345678901234567890" },
                { "Jwt:Issuer", "UmiHealthTest" },
                { "Jwt:Audience", "UmiHealthTestUsers" },
                { "Jwt:ExpiryMinutes", "60" }
            })
            .Build();

        _jwtService = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateToken_WithValidClaims_ShouldReturnToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.GivenName, "John"),
            new Claim(ClaimTypes.Surname, "Doe"),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("tenant_name", "Test Tenant")
        }));

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(token.Split('.').Length == 3); // JWT has 3 parts
    }

    [Fact]
    public void GetPrincipalFromToken_WithValidToken_ShouldReturnPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.GivenName, "John"),
            new Claim(ClaimTypes.Surname, "Doe"),
            new Claim("tenant_id", tenantId.ToString()),
            new Claim("tenant_name", "Test Tenant")
        }));

        var token = _jwtService.GenerateToken(claims);

        // Act
        var principal = _jwtService.GetPrincipalFromToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("test@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("John", principal.FindFirst(ClaimTypes.GivenName)?.Value);
        Assert.Equal("Doe", principal.FindFirst(ClaimTypes.Surname)?.Value);
        Assert.Equal(tenantId.ToString(), principal.FindFirst("tenant_id")?.Value);
    }

    [Fact]
    public void GetPrincipalFromToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var principal = _jwtService.GetPrincipalFromToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnUniqueToken()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        Assert.NotNull(token1);
        Assert.NotNull(token2);
        Assert.NotEmpty(token1);
        Assert.NotEmpty(token2);
        Assert.NotEqual(token1, token2);
        Assert.Contains("-", token1); // Format: GUID-GUID
        Assert.Contains("-", token2);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim("tenant_id", tenantId.ToString())
        }));

        var token = _jwtService.GenerateToken(claims);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange - Create a JWT service with very short expiry
        var shortExpiryConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Jwt:Key", "ThisIsASecretKeyForTesting12345678901234567890" },
                { "Jwt:Issuer", "UmiHealthTest" },
                { "Jwt:Audience", "UmiHealthTestUsers" },
                { "Jwt:ExpiryMinutes", "0" } // 0 minutes = immediate expiry
            })
            .Build();

        var shortExpiryService = new JwtService(shortExpiryConfig);

        var userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com")
        }));

        var token = shortExpiryService.GenerateToken(claims);

        // Wait a moment to ensure expiry
        System.Threading.Thread.Sleep(100);

        // Act
        var isValid = shortExpiryService.ValidateToken(token);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void CreateClaimsPrincipal_ShouldCreateCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var roles = new List<string> { "Admin", "Pharmacist" };

        // Act
        var principal = JwtService.CreateClaimsPrincipal(
            userId,
            "test@example.com",
            "John",
            "Doe",
            tenantId,
            "Test Tenant",
            branchId,
            "Main Branch",
            roles
        );

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("test@example.com", principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal("John", principal.FindFirst(ClaimTypes.GivenName)?.Value);
        Assert.Equal("Doe", principal.FindFirst(ClaimTypes.Surname)?.Value);
        Assert.Equal(tenantId.ToString(), principal.FindFirst("tenant_id")?.Value);
        Assert.Equal("Test Tenant", principal.FindFirst("tenant_name")?.Value);
        Assert.Equal(branchId.ToString(), principal.FindFirst("branch_id")?.Value);
        Assert.Equal("Main Branch", principal.FindFirst("branch_name")?.Value);
        
        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains("Admin", roleClaims);
        Assert.Contains("Pharmacist", roleClaims);
    }

    [Fact]
    public void CreateClaimsPrincipal_WithoutBranch_ShouldCreateCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roles = new List<string> { "User" };

        // Act
        var principal = JwtService.CreateClaimsPrincipal(
            userId,
            "test@example.com",
            "John",
            "Doe",
            tenantId,
            "Test Tenant",
            null,
            null,
            roles
        );

        // Assert
        Assert.NotNull(principal);
        Assert.Equal(userId.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(tenantId.ToString(), principal.FindFirst("tenant_id")?.Value);
        Assert.Null(principal.FindFirst("branch_id"));
        Assert.Null(principal.FindFirst("branch_name"));
    }
}
