using Microsoft.EntityFrameworkCore;
using Moq;
using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using UmiHealth.MinimalApi.Tests.TestHelpers;
using Xunit;
using FluentAssertions;

namespace UmiHealth.MinimalApi.Tests.Unit.Services
{
    public class UserServiceTests
    {
        private readonly UmiHealthDbContext _context;

        public UserServiceTests()
        {
            _context = TestDatabaseFactory.CreateInMemoryDatabase();
        }

        [Fact]
        public async Task CreateUser_ShouldAddUserToDatabase()
        {
            // Arrange
            var newUser = new User
            {
                Id = "new-user",
                Username = "newuser",
                Email = "newuser@test.com",
                Password = "hashed_password",
                FirstName = "New",
                LastName = "User",
                Role = "cashier",
                Status = "active",
                TenantId = "test-tenant-1",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Assert
            var createdUser = await _context.Users.FindAsync("new-user");
            createdUser.Should().NotBeNull();
            createdUser.Username.Should().Be("newuser");
            createdUser.Email.Should().Be("newuser@test.com");
        }

        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenUserExists()
        {
            // Arrange
            var userId = "user-1";

            // Act
            var user = await _context.Users.FindAsync(userId);

            // Assert
            user.Should().NotBeNull();
            user.Id.Should().Be(userId);
            user.Username.Should().Be("admin");
        }

        [Fact]
        public async Task GetUsersByTenant_ShouldReturnOnlyTenantUsers()
        {
            // Arrange
            var tenantId = "test-tenant-1";

            // Act
            var users = await _context.Users
                .Where(u => u.TenantId == tenantId)
                .ToListAsync();

            // Assert
            users.Should().NotBeEmpty();
            users.Should().OnlyContain(u => u.TenantId == tenantId);
        }

        [Fact]
        public async Task UpdateUser_ShouldModifyUserProperties()
        {
            // Arrange
            var userId = "user-1";
            var user = await _context.Users.FindAsync(userId);
            user.Should().NotBeNull();

            var originalEmail = user!.Email;
            var newEmail = "updated@test.com";

            // Act
            user.Email = newEmail;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Assert
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser.Should().NotBeNull();
            updatedUser.Email.Should().Be(newEmail);
            updatedUser.Email.Should().NotBe(originalEmail);
        }

        [Fact]
        public async Task DeleteUser_ShouldRemoveUserFromDatabase()
        {
            // Arrange
            var newUser = new User
            {
                Id = "user-to-delete",
                Username = "deleteuser",
                Email = "delete@test.com",
                Password = "hashed_password",
                FirstName = "Delete",
                LastName = "User",
                Role = "cashier",
                Status = "active",
                TenantId = "test-tenant-1",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Act
            _context.Users.Remove(newUser);
            await _context.SaveChangesAsync();

            // Assert
            var deletedUser = await _context.Users.FindAsync("user-to-delete");
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task GetActiveUsers_ShouldReturnOnlyActiveUsers()
        {
            // Arrange
            var tenantId = "test-tenant-1";

            // Act
            var activeUsers = await _context.Users
                .Where(u => u.TenantId == tenantId && u.Status == "active")
                .ToListAsync();

            // Assert
            activeUsers.Should().NotBeEmpty();
            activeUsers.Should().OnlyContain(u => u.Status == "active");
        }

        [Fact]
        public async Task GetUsersByRole_ShouldReturnOnlyUsersWithSpecifiedRole()
        {
            // Arrange
            var role = "admin";
            var tenantId = "test-tenant-1";

            // Act
            var adminUsers = await _context.Users
                .Where(u => u.TenantId == tenantId && u.Role == role)
                .ToListAsync();

            // Assert
            adminUsers.Should().NotBeEmpty();
            adminUsers.Should().OnlyContain(u => u.Role == role);
        }
    }
}
