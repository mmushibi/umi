using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealth.Core.Entities;
using UmiHealth.Persistence;

namespace UmiHealth.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<RolesController> _logger;

        public RolesController(UmiHealthDbContext context, ILogger<RolesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var roles = await _context.Roles
                    .Include(r => r.RoleClaims)
                    .Where(r => r.TenantId == currentUserTenantId.Value && !r.IsDeleted)
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        NormalizedName = r.NormalizedName,
                        Permissions = r.RoleClaims.Select(rc => new PermissionDto
                        {
                            ClaimType = rc.ClaimType,
                            ClaimValue = rc.ClaimValue
                        }).ToList(),
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { error = "Failed to retrieve roles" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetRole(Guid id)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var role = await _context.Roles
                    .Include(r => r.RoleClaims)
                    .FirstOrDefaultAsync(r => r.Id == id && 
                                             r.TenantId == currentUserTenantId.Value && 
                                             !r.IsDeleted);

                if (role == null)
                {
                    return NotFound();
                }

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    NormalizedName = role.NormalizedName,
                    Permissions = role.RoleClaims.Select(rc => new PermissionDto
                    {
                        ClaimType = rc.ClaimType,
                        ClaimValue = rc.ClaimValue
                    }).ToList(),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return Ok(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role {RoleId}", id);
                return StatusCode(500, new { error = "Failed to retrieve role" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                // Check if role already exists
                var existingRole = await _context.Roles
                    .AnyAsync(r => r.TenantId == currentUserTenantId.Value &&
                                   r.NormalizedName == request.Name.ToUpper() &&
                                   !r.IsDeleted);

                if (existingRole)
                {
                    return BadRequest(new { error = "Role with this name already exists" });
                }

                var role = new Role
                {
                    TenantId = currentUserTenantId.Value,
                    Name = request.Name,
                    Description = request.Description,
                    NormalizedName = request.Name.ToUpper()
                };

                // Add permissions if provided
                if (request.Permissions?.Any() == true)
                {
                    foreach (var permission in request.Permissions)
                    {
                        role.RoleClaims.Add(new RoleClaim
                        {
                            TenantId = currentUserTenantId.Value,
                            ClaimType = permission.ClaimType,
                            ClaimValue = permission.ClaimValue
                        });
                    }
                }

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Role created: {RoleId} - {RoleName}", role.Id, role.Name);

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    NormalizedName = role.NormalizedName,
                    Permissions = role.RoleClaims.Select(rc => new PermissionDto
                    {
                        ClaimType = rc.ClaimType,
                        ClaimValue = rc.ClaimValue
                    }).ToList(),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return CreatedAtAction(nameof(GetRole), new { id = role.Id }, roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, new { error = "Failed to create role" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var role = await _context.Roles
                    .Include(r => r.RoleClaims)
                    .FirstOrDefaultAsync(r => r.Id == id && 
                                             r.TenantId == currentUserTenantId.Value && 
                                             !r.IsDeleted);

                if (role == null)
                {
                    return NotFound();
                }

                // Update properties
                if (!string.IsNullOrEmpty(request.Name))
                    role.Name = request.Name;

                if (!string.IsNullOrEmpty(request.Description))
                    role.Description = request.Description;

                role.NormalizedName = request.Name?.ToUpper() ?? role.NormalizedName;

                // Update permissions if provided
                if (request.Permissions != null)
                {
                    // Remove existing permissions
                    _context.RoleClaims.RemoveRange(role.RoleClaims);

                    // Add new permissions
                    foreach (var permission in request.Permissions)
                    {
                        role.RoleClaims.Add(new RoleClaim
                        {
                            TenantId = currentUserTenantId.Value,
                            ClaimType = permission.ClaimType,
                            ClaimValue = permission.ClaimValue
                        });
                    }
                }

                role.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Role updated: {RoleId}", role.Id);

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    NormalizedName = role.NormalizedName,
                    Permissions = role.RoleClaims.Select(rc => new PermissionDto
                    {
                        ClaimType = rc.ClaimType,
                        ClaimValue = rc.ClaimValue
                    }).ToList(),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return Ok(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role {RoleId}", id);
                return StatusCode(500, new { error = "Failed to update role" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        public async Task<ActionResult> DeleteRole(Guid id)
        {
            try
            {
                var currentUserTenantId = GetCurrentUserTenantId();
                if (currentUserTenantId == null)
                {
                    return Unauthorized();
                }

                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Id == id && 
                                             r.TenantId == currentUserTenantId.Value && 
                                             !r.IsDeleted);

                if (role == null)
                {
                    return NotFound();
                }

                // Check if role is in use
                var usersWithRole = await _context.UserRoles
                    .AnyAsync(ur => ur.RoleId == id && !ur.IsDeleted);

                if (usersWithRole)
                {
                    return BadRequest(new { error = "Cannot delete role that is assigned to users" });
                }

                // Soft delete
                role.IsDeleted = true;
                role.DeletedAt = DateTime.UtcNow;
                role.DeletedBy = GetCurrentUserId()?.ToString();

                await _context.SaveChangesAsync();

                _logger.LogInformation("Role deleted: {RoleId}", role.Id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role {RoleId}", id);
                return StatusCode(500, new { error = "Failed to delete role" });
            }
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IEnumerable<StandardPermission>>> GetStandardPermissions()
        {
            try
            {
                var standardPermissions = new List<StandardPermission>
                {
                    new StandardPermission { Category = "Users", Name = "Create Users", ClaimType = "users", ClaimValue = "create" },
                    new StandardPermission { Category = "Users", Name = "Read Users", ClaimType = "users", ClaimValue = "read" },
                    new StandardPermission { Category = "Users", Name = "Update Users", ClaimType = "users", ClaimValue = "update" },
                    new StandardPermission { Category = "Users", Name = "Delete Users", ClaimType = "users", ClaimValue = "delete" },
                    
                    new StandardPermission { Category = "Patients", Name = "Create Patients", ClaimType = "patients", ClaimValue = "create" },
                    new StandardPermission { Category = "Patients", Name = "Read Patients", ClaimType = "patients", ClaimValue = "read" },
                    new StandardPermission { Category = "Patients", Name = "Update Patients", ClaimType = "patients", ClaimValue = "update" },
                    new StandardPermission { Category = "Patients", Name = "Delete Patients", ClaimType = "patients", ClaimValue = "delete" },
                    
                    new StandardPermission { Category = "Products", Name = "Create Products", ClaimType = "products", ClaimValue = "create" },
                    new StandardPermission { Category = "Products", Name = "Read Products", ClaimType = "products", ClaimValue = "read" },
                    new StandardPermission { Category = "Products", Name = "Update Products", ClaimType = "products", ClaimValue = "update" },
                    new StandardPermission { Category = "Products", Name = "Delete Products", ClaimType = "products", ClaimValue = "delete" },
                    
                    new StandardPermission { Category = "Inventory", Name = "Manage Inventory", ClaimType = "inventory", ClaimValue = "manage" },
                    new StandardPermission { Category = "Inventory", Name = "Stock Transfers", ClaimType = "inventory", ClaimValue = "transfer" },
                    
                    new StandardPermission { Category = "Prescriptions", Name = "Create Prescriptions", ClaimType = "prescriptions", ClaimValue = "create" },
                    new StandardPermission { Category = "Prescriptions", Name = "Read Prescriptions", ClaimType = "prescriptions", ClaimValue = "read" },
                    new StandardPermission { Category = "Prescriptions", Name = "Dispense Prescriptions", ClaimType = "prescriptions", ClaimValue = "dispense" },
                    
                    new StandardPermission { Category = "Sales", Name = "Create Sales", ClaimType = "sales", ClaimValue = "create" },
                    new StandardPermission { Category = "Sales", Name = "Read Sales", ClaimType = "sales", ClaimValue = "read" },
                    new StandardPermission { Category = "Sales", Name = "Process Returns", ClaimType = "sales", ClaimValue = "return" },
                    
                    new StandardPermission { Category = "Reports", Name = "View Reports", ClaimType = "reports", ClaimValue = "view" },
                    new StandardPermission { Category = "Reports", Name = "Export Reports", ClaimType = "reports", ClaimValue = "export" },
                    
                    new StandardPermission { Category = "Branches", Name = "Manage Branches", ClaimType = "branches", ClaimValue = "manage" },
                    new StandardPermission { Category = "Branches", Name = "Branch Transfers", ClaimType = "branches", ClaimValue = "transfer" }
                };

                return Ok(standardPermissions.GroupBy(p => p.Category));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving standard permissions");
                return StatusCode(500, new { error = "Failed to retrieve permissions" });
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

        private Guid? GetCurrentUserTenantId()
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                return tenantId;
            }
            return null;
        }
    }

    // DTOs
    public class RoleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string NormalizedName { get; set; } = string.Empty;
        public List<PermissionDto> Permissions { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PermissionDto
    {
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
    }

    public class StandardPermission
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ClaimType { get; set; } = string.Empty;
        public string ClaimValue { get; set; } = string.Empty;
    }

    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<PermissionDto>? Permissions { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<PermissionDto>? Permissions { get; set; }
    }
}
