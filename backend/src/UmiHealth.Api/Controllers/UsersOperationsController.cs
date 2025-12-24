using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Domain.Entities;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UsersOperationsController : ControllerBase
    {
        private readonly SharedDbContext _context;
        private readonly ILogger<UsersOperationsController> _logger;

        public UsersOperationsController(SharedDbContext context, ILogger<UsersOperationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null,
            [FromQuery] string? tenantId = null)
        {
            try
            {
                // Note: This would typically query a Users table, but for now we'll return mock data
                // based on the existing structure. In a real implementation, you'd have a Users entity.
                
                var users = new List<object>();
                
                // Mock data for demonstration
                for (int i = 1; i <= 20; i++)
                {
                    users.Add(new
                    {
                        Id = Guid.NewGuid(),
                        Username = $"user{i}",
                        Email = $"user{i}@example.com",
                        FirstName = $"User{i}",
                        LastName = $"Test",
                        Role = i % 3 == 0 ? "Admin" : i % 2 == 0 ? "Cashier" : "Operations",
                        Status = i % 4 == 0 ? "inactive" : "active",
                        TenantId = string.IsNullOrEmpty(tenantId) ? Guid.NewGuid() : Guid.Parse(tenantId),
                        BranchId = Guid.NewGuid(),
                        LastLogin = DateTime.UtcNow.AddDays(-i),
                        CreatedAt = DateTime.UtcNow.AddDays(-i * 10),
                        Permissions = new List<string> { "read", "write" }
                    });
                }

                if (!string.IsNullOrEmpty(search))
                {
                    users = users.Where(u => 
                        u.Email.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.FirstName.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.LastName.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Username.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(role))
                {
                    users = users.Where(u => u.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrEmpty(status))
                {
                    users = users.Where(u => u.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                var totalCount = users.Count;
                var pagedUsers = users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    users = pagedUsers,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(Guid id)
        {
            try
            {
                // Mock user data for demonstration
                var user = new
                {
                    Id = id,
                    Username = "testuser",
                    Email = "test@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    Role = "Cashier",
                    Status = "active",
                    TenantId = Guid.NewGuid(),
                    BranchId = Guid.NewGuid(),
                    PhoneNumber = "+1234567890",
                    Address = new
                    {
                        Street = "123 Test St",
                        City = "Test City",
                        Country = "Zambia",
                        PostalCode = "10101"
                    },
                    LastLogin = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    EmailVerified = true,
                    PhoneVerified = true,
                    TwoFactorEnabled = false,
                    Permissions = new List<string> { "read", "write", "delete" },
                    ProfilePicture = null,
                    Settings = new Dictionary<string, object>
                    {
                        ["theme"] = "light",
                        ["language"] = "en",
                        ["timezone"] = "Africa/Lusaka"
                    }
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateUser(CreateUserRequest request)
        {
            try
            {
                // Mock user creation for demonstration
                var user = new
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Role = request.Role,
                    Status = "active",
                    TenantId = request.TenantId ?? Guid.NewGuid(),
                    BranchId = request.BranchId ?? Guid.NewGuid(),
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    CreatedAt = DateTime.UtcNow,
                    EmailVerified = false,
                    PhoneVerified = false,
                    TwoFactorEnabled = false,
                    Permissions = request.Permissions ?? new List<string> { "read" },
                    Settings = request.Settings ?? new Dictionary<string, object>()
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
        {
            try
            {
                // Mock user update for demonstration
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                // Mock user deletion for demonstration
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<UserStats>> GetUserStats(
            [FromQuery] string? tenantId = null)
        {
            try
            {
                // Mock stats for demonstration
                return Ok(new UserStats
                {
                    TotalUsers = 150,
                    ActiveUsers = 135,
                    InactiveUsers = 15,
                    AdminUsers = 12,
                    CashierUsers = 85,
                    OperationsUsers = 53,
                    RecentUsers = 8,
                    OnlineUsers = 42
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user stats");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(Guid id, ResetPasswordRequest request)
        {
            try
            {
                // Mock password reset for demonstration
                return Ok(new { message = "Password reset email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(Guid id)
        {
            try
            {
                // Mock status toggle for demonstration
                return Ok(new { message = "User status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for user {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public Guid? TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public Dictionary<string, object>? Address { get; set; }
        public List<string>? Permissions { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Role { get; set; }
        public string? PhoneNumber { get; set; }
        public Dictionary<string, object>? Address { get; set; }
        public string? Status { get; set; }
        public List<string>? Permissions { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public bool SendEmailNotification { get; set; } = true;
    }

    public class UserStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int CashierUsers { get; set; }
        public int OperationsUsers { get; set; }
        public int RecentUsers { get; set; }
        public int OnlineUsers { get; set; }
    }
}
