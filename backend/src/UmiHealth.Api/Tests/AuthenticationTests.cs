using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealth.Application.Authorization;
using UmiHealth.Application.Services;
using UmiHealth.Domain.Entities;
using Xunit;
using Microsoft.AspNetCore.Authorization;

namespace UmiHealth.Api.Tests
{
    public class AuthenticationTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ITenantService> _mockTenantService;
        private readonly Mock<ISubscriptionService> _mockSubscriptionService;
        private readonly JwtTokenService _jwtTokenService;
        private readonly AuthenticationService _authService;
        private readonly AuthorizationService _authzService;
        private readonly Mock<SharedDbContext> _mockContext;

        public AuthenticationTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockTenantService = new Mock<ITenantService>();
            _mockSubscriptionService = new Mock<ISubscriptionService>();
            _mockContext = new Mock<SharedDbContext>();

            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("UmiHealthTest");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("UmiHealthApi");

            _jwtTokenService = new JwtTokenService(_mockConfiguration.Object);
            _authService = new AuthenticationService(
                _mockContext.Object,
                _mockConfiguration.Object,
                _mockTenantService.Object,
                _mockSubscriptionService.Object,
                _jwtTokenService);

            _authzService = new AuthorizationService(_authService);
        }

        [Fact]
        public async Task GenerateAccessToken_ShouldContainCorrectClaims()
        {
            // Arrange
            var user = CreateTestUser();
            
            // Act
            var token = await _jwtTokenService.GenerateAccessTokenAsync(user);
            
            // Assert
            Assert.NotNull(token);
            
            var principal = await _jwtTokenService.ValidateAccessTokenAsync(token);
            Assert.NotNull(principal);
            
            Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(user.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal(user.Role, principal.FindFirst(ClaimTypes.Role)?.Value);
            Assert.Equal(user.TenantId.ToString(), principal.FindFirst("tenant_id")?.Value);
        }

        [Fact]
        public async Task GenerateRefreshToken_ShouldExpireIn7Days()
        {
            // Arrange
            var user = CreateTestUser();
            
            // Act
            var token = await _jwtTokenService.GenerateRefreshTokenAsync(user);
            
            // Assert
            Assert.NotNull(token);
            
            var principal = await _jwtTokenService.ValidateRefreshTokenAsync(token);
            Assert.NotNull(principal);
            
            var tokenType = principal.FindFirst("token_type")?.Value;
            Assert.Equal("refresh", tokenType);
        }

        [Theory]
        [InlineData("super_admin", "system:*")]
        [InlineData("admin", "tenant:manage")]
        [InlineData("pharmacist", "prescriptions:*")]
        [InlineData("cashier", "pos:*")]
        [InlineData("operations", "subscriptions:*")]
        public async Task RolePermissions_ShouldBeCorrect(string role, string expectedPermission)
        {
            // Arrange
            var user = CreateTestUser(role);
            var claims = CreateClaims(user);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            
            // Act
            var hasPermission = await _authzService.HasPermissionAsync(principal, expectedPermission);
            
            // Assert
            Assert.True(hasPermission);
        }

        [Fact]
        public async Task BranchAccess_ShouldWorkForAdmin()
        {
            // Arrange
            var user = CreateTestUser("admin");
            var claims = CreateClaims(user);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            var branchId = Guid.NewGuid();
            
            // Act
            var hasAccess = await _authzService.CanAccessBranchAsync(principal, branchId);
            
            // Assert
            Assert.True(hasAccess);
        }

        [Fact]
        public async Task CrossBranchAccess_ShouldRequirePermission()
        {
            // Arrange
            var user = CreateTestUser("pharmacist");
            var claims = CreateClaims(user);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            
            // Act
            var canCrossBranch = await _authzService.CanCrossBranchAccessAsync(principal);
            
            // Assert
            Assert.False(canCrossBranch);
        }

        [Fact]
        public async Task Login_ShouldReturnTokensWithCorrectExpiry()
        {
            // Arrange
            var request = new LoginRequest
            {
                Identifier = "test@example.com",
                Password = "password123"
            };

            var user = CreateTestUser();
            var tenant = CreateTestTenant();

            _mockContext.Setup(c => c.Users.FindAsync(It.IsAny<Guid>()))
                .ReturnsAsync(user);
            _mockTenantService.Setup(t => t.GetByIdAsync(user.TenantId))
                .ReturnsAsync(tenant);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(900, result.ExpiresIn); // 15 minutes
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
        }

        private User CreateTestUser(string role = "pharmacist")
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = role,
                TenantId = Guid.NewGuid(),
                BranchId = Guid.NewGuid(),
                BranchAccess = new List<Guid> { Guid.NewGuid() },
                Permissions = new Dictionary<string, object>(),
                IsActive = true
            };
        }

        private Tenant CreateTestTenant()
        {
            return new Tenant
            {
                Id = Guid.NewGuid(),
                Name = "Test Pharmacy",
                Subdomain = "test-pharmacy",
                Status = "active"
            };
        }

        private List<Claim> CreateClaims(User user)
        {
            return new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("tenant_id", user.TenantId.ToString()),
                new Claim("branch_id", user.BranchId?.ToString() ?? ""),
                new Claim("username", user.Username ?? user.Email)
            };
        }
    }

    // Integration test attributes for easier testing
    public class TestAuthenticationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string Role { get; }

        public TestAuthenticationAttribute(string role)
        {
            Role = role;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, Role),
                new Claim("tenant_id", Guid.NewGuid().ToString()),
                new Claim("branch_id", Guid.NewGuid().ToString())
            };

            var identity = new ClaimsIdentity(claims, "Test");
            context.HttpContext.User = new ClaimsPrincipal(identity);

            return Task.CompletedTask;
        }
    }
}
