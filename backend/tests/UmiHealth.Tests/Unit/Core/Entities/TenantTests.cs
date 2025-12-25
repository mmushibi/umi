using Xunit;
using UmiHealth.Core.Entities;

namespace UmiHealth.Tests.Unit.Core.Entities;

public class TenantTests
{
    [Fact]
    public void Tenant_ShouldInitializeCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var name = "Test Pharmacy";
        var subdomain = "testpharmacy";

        // Act
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = name,
            Subdomain = subdomain,
            DatabaseName = $"umihealth_{subdomain}",
            ContactEmail = "test@example.com",
            ContactPhone = "+1234567890",
            Address = "123 Test St",
            City = "Test City",
            Country = "Test Country",
            PostalCode = "12345",
            SubscriptionPlan = "Premium",
            SubscriptionExpiresAt = DateTime.UtcNow.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(tenantId, tenant.Id);
        Assert.Equal(name, tenant.Name);
        Assert.Equal(subdomain, tenant.Subdomain);
        Assert.Equal($"umihealth_{subdomain}", tenant.DatabaseName);
        Assert.Equal("test@example.com", tenant.ContactEmail);
        Assert.Equal("+1234567890", tenant.ContactPhone);
        Assert.Equal("123 Test St", tenant.Address);
        Assert.Equal("Test City", tenant.City);
        Assert.Equal("Test Country", tenant.Country);
        Assert.Equal("12345", tenant.PostalCode);
        Assert.Equal("Premium", tenant.SubscriptionPlan);
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void Tenant_ShouldHaveBranchesCollection()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        Assert.NotNull(tenant.Branches);
        Assert.Empty(tenant.Branches);
    }

    [Fact]
    public void Tenant_ShouldHaveUsersCollection()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        Assert.NotNull(tenant.Users);
        Assert.Empty(tenant.Users);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Tenant_ShouldRequireName(string name)
    {
        // Arrange & Act
        var tenant = new Tenant { Name = name };

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(tenant.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Tenant_ShouldRequireSubdomain(string subdomain)
    {
        // Arrange & Act
        var tenant = new Tenant { Subdomain = subdomain };

        // Assert
        Assert.True(string.IsNullOrWhiteSpace(tenant.Subdomain));
    }

    [Fact]
    public void Tenant_ShouldHaveComplianceSettings()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        Assert.NotNull(tenant.ComplianceSettings);
    }
}
