using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using UmiHealth.Application.Services;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Tests.Unit.Application.Services
{
    /// <summary>
    /// Base class for service tests providing common setup and utilities
    /// </summary>
    public abstract class ServiceTestBase : IDisposable
    {
        protected readonly Mock<ILogger<IServiceType>> MockLogger;
        protected readonly DbContextOptions<SharedDbContext> DbContextOptions;

        protected ServiceTestBase()
        {
            // Configure in-memory database
            DbContextOptions = new DbContextOptionsBuilder<SharedDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            MockLogger = new Mock<ILogger<IServiceType>>();
        }

        /// <summary>
        /// Creates a fresh DbContext for testing
        /// </summary>
        protected SharedDbContext CreateContext()
        {
            return new SharedDbContext(DbContextOptions);
        }

        /// <summary>
        /// Seeds test data into the database
        /// </summary>
        protected async Task SeedTestDataAsync(Func<SharedDbContext, Task> seedFunc)
        {
            using (var context = CreateContext())
            {
                await seedFunc(context);
                await context.SaveChangesAsync();
            }
        }

        public virtual void Dispose()
        {
            // Cleanup if needed
        }
    }

    /// <summary>
    /// Tests for AuthenticationService
    /// </summary>
    public class AuthenticationServiceTests : ServiceTestBase
    {
        private readonly Mock<ITokenGenerator> _mockTokenGenerator;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;

        public AuthenticationServiceTests()
        {
            _mockTokenGenerator = new Mock<ITokenGenerator>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessfulLogin()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var email = "test@example.com";
            var password = "ValidPassword123!";
            var passwordHash = "hashedpassword";

            var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" };
            var user = new User
            {
                Id = userId,
                TenantId = tenantId,
                Email = email,
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                IsActive = true
            };

            _mockPasswordHasher.Setup(x => x.VerifyHashedPassword(passwordHash, password))
                .Returns(PasswordVerificationResult.Success);
            _mockTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("jwt-token");
            _mockTokenGenerator.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh-token");

            await SeedTestDataAsync(async context =>
            {
                context.Tenants.Add(tenant);
                context.Users.Add(user);
            });

            var service = new AuthenticationService(
                CreateContext(),
                _mockTokenGenerator.Object,
                _mockPasswordHasher.Object,
                MockLogger.Object);

            // Act
            var result = await service.LoginAsync(new LoginRequest { Email = email, Password = password, TenantSubdomain = "test-tenant" });

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Token);
            Assert.Equal(email, result.User.Email);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_ReturnsFailed()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var email = "test@example.com";
            var password = "WrongPassword";
            var passwordHash = "hashedpassword";

            var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" };
            var user = new User
            {
                Id = userId,
                TenantId = tenantId,
                Email = email,
                PasswordHash = passwordHash,
                FirstName = "Test",
                LastName = "User",
                IsActive = true
            };

            _mockPasswordHasher.Setup(x => x.VerifyHashedPassword(passwordHash, password))
                .Returns(PasswordVerificationResult.Failed);

            await SeedTestDataAsync(async context =>
            {
                context.Tenants.Add(tenant);
                context.Users.Add(user);
            });

            var service = new AuthenticationService(
                CreateContext(),
                _mockTokenGenerator.Object,
                _mockPasswordHasher.Object,
                MockLogger.Object);

            // Act
            var result = await service.LoginAsync(new LoginRequest { Email = email, Password = password, TenantSubdomain = "test-tenant" });

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Invalid", result.Message);
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ReturnsFailed()
        {
            // Arrange
            var email = "nonexistent@example.com";
            var password = "ValidPassword123!";

            var service = new AuthenticationService(
                CreateContext(),
                _mockTokenGenerator.Object,
                _mockPasswordHasher.Object,
                MockLogger.Object);

            // Act
            var result = await service.LoginAsync(new LoginRequest { Email = email, Password = password, TenantSubdomain = "test-tenant" });

            // Assert
            Assert.False(result.Success);
            Assert.Contains("not found", result.Message);
        }
    }

    /// <summary>
    /// Tests for BranchInventoryService
    /// </summary>
    public class BranchInventoryServiceTests : ServiceTestBase
    {
        [Fact]
        public async Task GetBranchInventoryAsync_ReturnsCorrectInventory()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" };
            var branch = new Branch { Id = branchId, TenantId = tenantId, Name = "Main Branch", Code = "MB" };
            var product = new Product 
            { 
                Id = productId, 
                TenantId = tenantId, 
                Name = "Paracetamol", 
                SellingPrice = 500,
                UnitCost = 300
            };
            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branchId,
                ProductId = productId,
                QuantityOnHand = 100,
                QuantityReserved = 10,
                ExpiryDate = DateTime.UtcNow.AddYears(1)
            };

            await SeedTestDataAsync(async context =>
            {
                context.Tenants.Add(tenant);
                context.Branches.Add(branch);
                context.Products.Add(product);
                context.Inventories.Add(inventory);
            });

            var service = new BranchInventoryService(CreateContext(), MockLogger.Object);

            // Act
            var result = await service.GetBranchInventoryAsync(tenantId, branchId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(100, result.First().QuantityOnHand);
        }

        [Fact]
        public async Task ReserveInventoryAsync_SuccessfullReservesStock()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" };
            var branch = new Branch { Id = branchId, TenantId = tenantId, Name = "Main Branch", Code = "MB" };
            var product = new Product 
            { 
                Id = productId, 
                TenantId = tenantId, 
                Name = "Paracetamol", 
                SellingPrice = 500
            };
            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branchId,
                ProductId = productId,
                QuantityOnHand = 100,
                QuantityReserved = 0
            };

            await SeedTestDataAsync(async context =>
            {
                context.Tenants.Add(tenant);
                context.Branches.Add(branch);
                context.Products.Add(product);
                context.Inventories.Add(inventory);
            });

            var service = new BranchInventoryService(CreateContext(), MockLogger.Object);

            // Act
            var result = await service.ReserveInventoryAsync(tenantId, branchId, productId, 20);

            // Assert
            Assert.True(result);
            using (var context = CreateContext())
            {
                var updatedInventory = await context.Inventories.FirstAsync();
                Assert.Equal(20, updatedInventory.QuantityReserved);
            }
        }

        [Fact]
        public async Task ReserveInventoryAsync_WithInsufficientStock_ReturnsFalse()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var tenant = new Tenant { Id = tenantId, Name = "Test Tenant", Slug = "test-tenant" };
            var branch = new Branch { Id = branchId, TenantId = tenantId, Name = "Main Branch", Code = "MB" };
            var product = new Product 
            { 
                Id = productId, 
                TenantId = tenantId, 
                Name = "Paracetamol", 
                SellingPrice = 500
            };
            var inventory = new Inventory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BranchId = branchId,
                ProductId = productId,
                QuantityOnHand = 10,
                QuantityReserved = 0
            };

            await SeedTestDataAsync(async context =>
            {
                context.Tenants.Add(tenant);
                context.Branches.Add(branch);
                context.Products.Add(product);
                context.Inventories.Add(inventory);
            });

            var service = new BranchInventoryService(CreateContext(), MockLogger.Object);

            // Act
            var result = await service.ReserveInventoryAsync(tenantId, branchId, productId, 20);

            // Assert
            Assert.False(result);
        }
    }
}
