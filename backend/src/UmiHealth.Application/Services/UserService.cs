using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Application.DTOs;
using UmiHealth.Shared.DTOs;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Identity.Services;
using UmiHealth.Application.Adapters;

namespace UmiHealth.Application.Services
{
    public class UserService : IUserService
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IPasswordService _passwordService;

        public UserService(UmiHealthDbContext context, ILogger<UserService> logger, IPasswordService passwordService)
        {
            _context = context;
            _logger = logger;
            _passwordService = passwordService;
        }

        public async Task<IPagedResult<IUserDto>> GetUsersAsync(Guid tenantId, int page = 1, int limit = 50, string? search = null, string? role = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.TenantId == tenantId)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u => 
                        u.FirstName.Contains(search) || 
                        u.LastName.Contains(search) || 
                        u.Email.Contains(search));
                }

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name == role));
                }

                var totalCount = await query.CountAsync(cancellationToken);
                var users = await query
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(u => new UserDtoAdapter
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber ?? "",
                        TenantId = u.TenantId,
                        TenantName = (u.Tenant != null ? u.Tenant.Name : ""),
                        BranchId = u.BranchId,
                        BranchName = (u.Branch != null ? u.Branch.Name : null),
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                        LastLoginAt = u.LastLoginAt ?? DateTime.UtcNow,
                        IsActive = u.IsActive
                    })
                    .ToListAsync(cancellationToken);

                return new PagedResultAdapter<IUserDto>
                {
                    Data = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = limit,
                    TotalPages = (int)Math.Ceiling((double)totalCount / limit),
                    HasNextPage = page * limit < totalCount,
                    HasPreviousPage = page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<IUserDto?> GetUserByIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId && u.TenantId == tenantId)
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Include(u => u.Tenant)
                    .FirstOrDefaultAsync(cancellationToken);

                if (user == null)
                    return null;

                return new UserDtoAdapter
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "",
                    TenantId = user.TenantId,
                    TenantName = user.Tenant?.Name ?? "",
                    BranchId = user.BranchId,
                    BranchName = user.Branch?.Name,
                    Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                    LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow,
                    IsActive = user.IsActive
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId} for tenant {TenantId}", userId, tenantId);
                throw;
            }
        }

        public async Task<IUserDto> CreateUserAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if email is unique
                if (!await IsEmailUniqueAsync(tenantId, request.Email, null, cancellationToken))
                {
                    throw new InvalidOperationException($"Email {request.Email} is already in use");
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                    PhoneNumber = request.Phone,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Set password if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.PasswordHash = _passwordService.HashPassword(request.Password);
                }

                // Assign branch if specified
                if (request.BranchId.HasValue)
                {
                    user.BranchId = request.BranchId.Value;
                }

                // Assign role
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == request.Role && r.TenantId == tenantId, cancellationToken);
                
                if (role != null)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        RoleId = role.Id
                    });
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync(cancellationToken);

                // Reload with includes
                var createdUser = await GetUserByIdAsync(user.Id, tenantId, cancellationToken);
                if (createdUser == null)
                    throw new InvalidOperationException("Failed to retrieve created user");

                _logger.LogInformation("Created user {UserId} with email {Email} for tenant {TenantId}", user.Id, user.Email, tenantId);
                return createdUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<IUserDto> UpdateUserAsync(Guid userId, Guid tenantId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

                if (user == null)
                    throw new KeyNotFoundException($"User {userId} not found");

                // Check if email is unique (excluding current user)
                if (!await IsEmailUniqueAsync(tenantId, request.Email, userId, cancellationToken))
                {
                    throw new InvalidOperationException($"Email {request.Email} is already in use");
                }

                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Email = request.Email;
                user.PhoneNumber = request.Phone;
                user.IsActive = request.Status == "active";
                user.UpdatedAt = DateTime.UtcNow;

                // Update branch if specified
                if (request.BranchId.HasValue)
                {
                    user.BranchId = request.BranchId.Value;
                }

                // Update role if changed
                var newRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == request.Role && r.TenantId == tenantId, cancellationToken);
                
                if (newRole != null)
                {
                    // Remove existing roles
                    user.UserRoles.Clear();
                    
                    // Add new role
                    user.UserRoles.Add(new UserRole
                    {
                        RoleId = newRole.Id
                    });
                }

                await _context.SaveChangesAsync(cancellationToken);

                var updatedUser = await GetUserByIdAsync(userId, tenantId, cancellationToken);
                if (updatedUser == null)
                    throw new InvalidOperationException("Failed to retrieve updated user");

                _logger.LogInformation("Updated user {UserId} for tenant {TenantId}", userId, tenantId);
                return updatedUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId} for tenant {TenantId}", userId, tenantId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

                if (user == null)
                    return false;

                // Soft delete by deactivating
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                user.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted user {UserId} for tenant {TenantId}", userId, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId} for tenant {TenantId}", userId, tenantId);
                throw;
            }
        }

        public async Task<bool> UpdateUserStatusAsync(Guid userId, Guid tenantId, string status, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);

                if (user == null)
                    return false;

                user.IsActive = status == "active";
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated status for user {UserId} to {Status} for tenant {TenantId}", userId, status, tenantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for user {UserId} for tenant {TenantId}", userId, tenantId);
                throw;
            }
        }

        public async Task<IEnumerable<IUserDto>> GetUsersByRoleAsync(Guid tenantId, string role, CancellationToken cancellationToken = default)
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.TenantId == tenantId && u.UserRoles.Any(ur => ur.Role.Name == role))
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Select(u => new UserDtoAdapter
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber ?? "",
                        TenantId = u.TenantId,
                        TenantName = (u.Tenant != null ? u.Tenant.Name : ""),
                        BranchId = u.BranchId,
                        BranchName = (u.Branch != null ? u.Branch.Name : null),
                        Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                        LastLoginAt = u.LastLoginAt ?? DateTime.UtcNow,
                        IsActive = u.IsActive
                    })
                    .ToListAsync(cancellationToken);

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role {Role} for tenant {TenantId}", role, tenantId);
                throw;
            }
        }

        public async Task<bool> IsEmailUniqueAsync(Guid tenantId, string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.TenantId == tenantId && u.Email.ToLower() == email.ToLower());

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                var existingUser = await query.FirstOrDefaultAsync(cancellationToken);
                return existingUser == null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email uniqueness for {Email} in tenant {TenantId}", email, tenantId);
                throw;
            }
        }
    }
}
