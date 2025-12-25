using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Core.Entities;
using UmiHealth.Infrastructure;
using UmiHealth.Identity;

namespace UmiHealth.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UmiHealthDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UmiHealthDbContext context,
            IPasswordHasher passwordHasher,
            ILogger<UsersController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] Guid? branchId = null)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUserTenantId = GetCurrentUserTenantId();

                if (currentUserId == null || currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var query = _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Include(u => u.Branch)
                    .Where(u => u.TenantId == currentUserTenantId.Value && !u.IsDeleted);

                // Apply role-based filtering
                if (currentUserRole != "Admin" && currentUserRole != "SuperAdmin")
                {
                    // Non-admin users can only see users in their branch
                    var userBranchId = GetCurrentUserBranchId();
                    query = query.Where(u => u.BranchId == userBranchId || u.BranchId == null);
                }

                if (branchId.HasValue)
                {
                    query = query.Where(u => u.BranchId == branchId.Value);
                }

                var users = await query
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        UserName = u.UserName,
                        IsActive = u.IsActive,
                        EmailConfirmed = u.EmailConfirmed,
                        PhoneNumberConfirmed = u.PhoneNumberConfirmed,
                        TwoFactorEnabled = u.TwoFactorEnabled,
                        LastLoginAt = u.LastLoginAt,
                        FailedLoginAttempts = u.FailedLoginAttempts,
                        LockoutEnd = u.LockoutEnd,
                        TenantId = u.TenantId,
                        BranchId = u.BranchId,
                        BranchName = u.Branch != null ? u.Branch.Name : null,
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { error = "Failed to retrieve users" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(Guid id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUserTenantId = GetCurrentUserTenantId();

                if (currentUserId == null || currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                // Users can only see their own profile unless admin
                if (currentUserRole != "Admin" && currentUserRole != "SuperAdmin" && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Include(u => u.Branch)
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId.Value && !u.IsDeleted);

                if (user == null)
                {
                    return NotFound();
                }

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.UserName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LastLoginAt = user.LastLoginAt,
                    FailedLoginAttempts = user.FailedLoginAttempts,
                    LockoutEnd = user.LockoutEnd,
                    TenantId = user.TenantId,
                    BranchId = user.BranchId,
                    BranchName = user.Branch?.Name,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", id);
                return StatusCode(500, new { error = "Failed to retrieve user" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                // Check if user already exists
                var existingUser = await _context.Users
                    .AnyAsync(u => u.TenantId == currentUserTenantId.Value &&
                                   (u.Email.ToLower() == request.Email.ToLower() || 
                                    u.UserName.ToLower() == request.UserName.ToLower()) &&
                                   !u.IsDeleted);

                if (existingUser)
                {
                    return BadRequest(new { error = "User with this email or username already exists" });
                }

                // Validate branch exists if specified
                if (request.BranchId.HasValue)
                {
                    var branch = await _context.Branches
                        .FirstOrDefaultAsync(b => b.Id == request.BranchId.Value && 
                                             b.TenantId == currentUserTenantId.Value && 
                                             !b.IsDeleted);

                    if (branch == null)
                    {
                        return BadRequest(new { error = "Invalid branch" });
                    }
                }

                var user = new User
                {
                    TenantId = currentUserTenantId.Value,
                    BranchId = request.BranchId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    UserName = request.UserName,
                    PasswordHash = _passwordHasher.HashPassword(request.Password),
                    IsActive = true,
                    EmailConfirmed = false,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    FailedLoginAttempts = 0
                };

                // Assign roles
                if (request.RoleIds?.Any() == true)
                {
                    var roles = await _context.Roles
                        .Where(r => request.RoleIds.Contains(r.Id) && 
                                     r.TenantId == currentUserTenantId.Value && 
                                     !r.IsDeleted)
                        .ToListAsync();

                    foreach (var role in roles)
                    {
                        user.UserRoles.Add(new UserRole
                        {
                            TenantId = currentUserTenantId.Value,
                            RoleId = role.Id
                        });
                    }
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created: {UserId} - {UserName}", user.Id, user.UserName);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.UserName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    TenantId = user.TenantId,
                    BranchId = user.BranchId,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { error = "Failed to create user" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<UserDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUserRole = GetCurrentUserRole();
                var currentUserTenantId = GetCurrentUserTenantId();

                if (currentUserId == null || currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                // Users can only update their own profile unless admin
                if (currentUserRole != "Admin" && currentUserRole != "SuperAdmin" && currentUserId != id)
                {
                    return Forbid();
                }

                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId.Value && !u.IsDeleted);

                if (user == null)
                {
                    return NotFound();
                }

                // Update allowed fields
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;

                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                if (!string.IsNullOrEmpty(request.PhoneNumber))
                    user.PhoneNumber = request.PhoneNumber;

                // Only admins can change certain fields
                if (currentUserRole == "Admin" || currentUserRole == "SuperAdmin")
                {
                    if (!string.IsNullOrEmpty(request.Email))
                        user.Email = request.Email;

                    if (request.BranchId.HasValue)
                    {
                        var branch = await _context.Branches
                            .FirstOrDefaultAsync(b => b.Id == request.BranchId.Value && 
                                                 b.TenantId == currentUserTenantId.Value && 
                                                 !b.IsDeleted);

                        if (branch != null)
                        {
                            user.BranchId = request.BranchId.Value;
                        }
                    }

                    if (request.RoleIds?.Any() == true)
                    {
                        // Remove existing roles
                        _context.UserRoles.RemoveRange(user.UserRoles);

                        // Add new roles
                        var roles = await _context.Roles
                            .Where(r => request.RoleIds.Contains(r.Id) && 
                                         r.TenantId == currentUserTenantId.Value && 
                                         !r.IsDeleted)
                            .ToListAsync();

                        foreach (var role in roles)
                        {
                            user.UserRoles.Add(new UserRole
                            {
                                TenantId = currentUserTenantId.Value,
                                RoleId = role.Id
                            });
                        }
                    }

                    user.IsActive = request.IsActive;
                }

                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated: {UserId}", user.Id);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.UserName,
                    IsActive = user.IsActive,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    TenantId = user.TenantId,
                    BranchId = user.BranchId,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { error = "Failed to update user" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId.Value && !u.IsDeleted);

                if (user == null)
                {
                    return NotFound();
                }

                // Soft delete
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.DeletedBy = GetCurrentUserId()?.ToString();

                await _context.SaveChangesAsync();

                _logger.LogInformation("User deleted: {UserId}", user.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { error = "Failed to delete user" });
            }
        }

        [HttpPost("{id}/reset-password")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult> ResetUserPassword(Guid id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId.Value && !u.IsDeleted);

                if (user == null)
                {
                    return NotFound();
                }

                user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user: {UserId}", user.Id);

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", id);
                return StatusCode(500, new { error = "Failed to reset password" });
            }
        }

        [HttpPost("{id}/toggle-status")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult> ToggleUserStatus(Guid id)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == currentUserTenantId.Value && !u.IsDeleted);

                if (user == null)
                {
                    return NotFound();
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User status toggled: {UserId} - Active: {IsActive}", user.Id, user.IsActive);

                return Ok(new { 
                    userId = user.Id, 
                    isActive = user.IsActive,
                    message = user.IsActive ? "User activated" : "User deactivated"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for user {UserId}", id);
                return StatusCode(500, new { error = "Failed to toggle user status" });
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }

        private string? GetCurrentUserRole()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        }

        private Guid? GetCurrentUserTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                return tenantId;
            }
            return null;
        }

        private Guid? GetCurrentUserBranchId()
        {
            var branchIdClaim = User.FindFirst("branch_id");
            if (branchIdClaim != null && Guid.TryParse(branchIdClaim.Value, out var branchId))
            {
                return branchId;
            }
            return null;
        }
    }

    // DTOs
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public Guid TenantId { get; set; }
        public Guid? BranchId { get; set; }
        public string? BranchName { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
        public List<Guid>? RoleIds { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public Guid? BranchId { get; set; }
        public List<Guid>? RoleIds { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }
}
