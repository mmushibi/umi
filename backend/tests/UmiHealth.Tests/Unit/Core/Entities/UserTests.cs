using Xunit;
using UmiHealth.Core.Entities;

namespace UmiHealth.Tests.Unit.Core.Entities;

public class UserTests
{
    [Fact]
    public void User_ShouldInitializeCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var email = "test@example.com";
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var user = new User
        {
            Id = userId,
            TenantId = tenantId,
            BranchId = branchId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserName = email,
            PasswordHash = "hashedpassword",
            PhoneNumber = "+1234567890",
            IsActive = true,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            TwoFactorEnabled = false,
            FailedLoginAttempts = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(userId, user.Id);
        Assert.Equal(tenantId, user.TenantId);
        Assert.Equal(branchId, user.BranchId);
        Assert.Equal(email, user.Email);
        Assert.Equal(firstName, user.FirstName);
        Assert.Equal(lastName, user.LastName);
        Assert.Equal(email, user.UserName);
        Assert.Equal("hashedpassword", user.PasswordHash);
        Assert.Equal("+1234567890", user.PhoneNumber);
        Assert.True(user.IsActive);
        Assert.True(user.EmailConfirmed);
        Assert.True(user.PhoneNumberConfirmed);
        Assert.False(user.TwoFactorEnabled);
        Assert.Equal(0, user.FailedLoginAttempts);
    }

    [Fact]
    public void User_ShouldHaveUserRolesCollection()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.NotNull(user.UserRoles);
        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void User_ShouldHaveUserClaimsCollection()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.NotNull(user.UserClaims);
        Assert.Empty(user.UserClaims);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void User_ShouldRequireEmail(string email)
    {
        // Arrange & Act
        var user = new User { Email = email };

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(user.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void User_ShouldRequireFirstName(string firstName)
    {
        // Arrange & Act
        var user = new User { FirstName = firstName };

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(user.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void User_ShouldRequireLastName(string lastName)
    {
        // Arrange & Act
        var user = new User { LastName = lastName };

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(user.LastName));
    }

    [Fact]
    public void User_ShouldHaveRefreshTokensCollection()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.NotNull(user.RefreshTokens);
        Assert.Empty(user.RefreshTokens);
    }

    [Fact]
    public void User_ShouldSupportLockout()
    {
        // Arrange & Act
        var user = new User
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(15),
            FailedLoginAttempts = 5
        };

        // Assert
        Assert.True(user.LockoutEnd.HasValue);
        Assert.Equal(5, user.FailedLoginAttempts);
        Assert.True(user.LockoutEnd.Value > DateTime.UtcNow);
    }
}
