using Xunit;
using Moq;
using System.Threading.Tasks;
using UmiHealth.Core.Interfaces;
using UmiHealth.Application.Services;
using UmiHealth.Core.Entities;

namespace UmiHealth.Tests.Unit.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<ITenantRepository<User>> _userRepositoryMock;
    private readonly Mock<ITenantRepository<Role>> _roleRepositoryMock;
    private readonly Mock<ITenantRepository<UserRole>> _userRoleRepositoryMock;
    private readonly Mock<ITenantRepository<RefreshToken>> _refreshTokenRepositoryMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<ITenantRepository<User>>();
        _roleRepositoryMock = new Mock<ITenantRepository<Role>>();
        _userRoleRepositoryMock = new Mock<ITenantRepository<UserRole>>();
        _refreshTokenRepositoryMock = new Mock<ITenantRepository<RefreshToken>>();
        _tenantServiceMock = new Mock<ITenantService>();

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _userRoleRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _tenantServiceMock.Object
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "password123";
        var tenant = new Tenant { Id = tenantId, IsActive = true };
        var user = new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = email,
            PasswordHash = "hashedpassword",
            IsActive = true,
            FailedLoginAttempts = 0
        };

        _tenantServiceMock.Setup(x => x.GetTenantBySubdomainAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _tenantServiceMock.Setup(x => x.IsSubscriptionActiveAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        // Act
        var result = await _authService.LoginAsync(new(email, password, "test"));

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.User);
        Assert.Equal(userId, result.User.Id);
        Assert.Equal(email, result.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, IsActive = true };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            IsActive = true,
            FailedLoginAttempts = 0
        };

        _tenantServiceMock.Setup(x => x.GetTenantBySubdomainAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _tenantServiceMock.Setup(x => x.IsSubscriptionActiveAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });

        // Act
        var result = await _authService.LoginAsync(new("test@example.com", "wrongpassword", "test"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid credentials", result.Message);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task LoginAsync_WithInactiveTenant_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, IsActive = false };

        _tenantServiceMock.Setup(x => x.GetTenantBySubdomainAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _authService.LoginAsync(new("test@example.com", "password123", "test"));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid tenant or subscription expired", result.Message);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, IsActive = true };
        var role = new Role { Id = roleId, TenantId = tenantId, Name = "User" };

        _tenantServiceMock.Setup(x => x.IsSubscriptionActiveAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        _roleRepositoryMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Role, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role> { role });
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId });
        _userRoleRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserRole());

        // Act
        var result = await _authService.RegisterAsync(new(
            "test@example.com",
            "password123",
            "John",
            "Doe",
            "+1234567890",
            tenantId,
            null
        ));

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Registration successful", result.Message);
        Assert.NotNull(result.User);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existingUser = new User { Email = "test@example.com" };
        var tenant = new Tenant { Id = tenantId, IsActive = true };

        _tenantServiceMock.Setup(x => x.IsSubscriptionActiveAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userRepositoryMock.Setup(x => x.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { existingUser });

        // Act
        var result = await _authService.RegisterAsync(new(
            "test@example.com",
            "password123",
            "John",
            "Doe",
            "+1234567890",
            tenantId,
            null
        ));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User with this email already exists", result.Message);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = "oldhashedpassword"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _authService.ChangePasswordAsync(userId, new(
            "oldpassword",
            "newpassword",
            "newpassword"
        ));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            PasswordHash = "oldhashedpassword"
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.ChangePasswordAsync(userId, new(
            "wrongpassword",
            "newpassword",
            "newpassword"
        ));

        // Assert
        Assert.False(result);
    }
}
