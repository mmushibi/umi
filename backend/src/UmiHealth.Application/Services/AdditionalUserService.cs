using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Entities;
using UmiHealth.Core.Interfaces;
using UmiHealth.Persistence;
using UmiHealth.Domain.Entities;

namespace UmiHealth.Application.Services
{
    public interface IAdditionalUserService
    {
        Task<bool> AssignAdditionalUserAsync(Guid mainUserId, Guid additionalUserId, CancellationToken cancellationToken = default);
        Task<bool> RemoveAdditionalUserAsync(Guid mainUserId, Guid additionalUserId, CancellationToken cancellationToken = default);
        Task<IEnumerable<UmiHealth.Domain.Entities.User>> GetAdditionalUsersAsync(Guid mainUserId, CancellationToken cancellationToken = default);
        Task<UmiHealth.Domain.Entities.User?> GetMainUserAsync(Guid additionalUserId, CancellationToken cancellationToken = default);
        Task<bool> CanAccessUserDataAsync(Guid requestingUserId, Guid targetUserId, CancellationToken cancellationToken = default);
    }

    public class AdditionalUserService : IAdditionalUserService
    {
        private readonly UmiHealthDbContext _context;
        private readonly ILogger<AdditionalUserService> _logger;

        public AdditionalUserService(UmiHealthDbContext context, ILogger<AdditionalUserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AssignAdditionalUserAsync(Guid mainUserId, Guid additionalUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if both users exist and are in the same tenant
                var mainUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == mainUserId, cancellationToken);
                
                var additionalUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == additionalUserId, cancellationToken);

                if (mainUser == null || additionalUser == null)
                {
                    _logger.LogWarning("Cannot assign additional user: one or both users not found");
                    return false;
                }

                if (mainUser.TenantId != additionalUser.TenantId)
                {
                    _logger.LogWarning("Cannot assign additional user: users are in different tenants");
                    return false;
                }

                // Check if relationship already exists
                var existingRelation = await _context.UserAdditionalUsers
                    .AnyAsync(ua => ua.MainUserId == mainUserId && ua.AdditionalUserId == additionalUserId, cancellationToken);

                if (existingRelation)
                {
                    _logger.LogWarning("Additional user relationship already exists");
                    return false;
                }

                // Create the relationship
                var userAdditionalUser = new UserAdditionalUser
                {
                    Id = Guid.NewGuid(),
                    MainUserId = mainUserId,
                    AdditionalUserId = additionalUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserAdditionalUsers.Add(userAdditionalUser);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Assigned additional user {AdditionalUserId} to main user {MainUserId}", 
                    additionalUserId, mainUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning additional user {AdditionalUserId} to main user {MainUserId}", 
                    additionalUserId, mainUserId);
                return false;
            }
        }

        public async Task<bool> RemoveAdditionalUserAsync(Guid mainUserId, Guid additionalUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var relationship = await _context.UserAdditionalUsers
                    .FirstOrDefaultAsync(ua => ua.MainUserId == mainUserId && ua.AdditionalUserId == additionalUserId, cancellationToken);

                if (relationship == null)
                {
                    _logger.LogWarning("Additional user relationship not found");
                    return false;
                }

                _context.UserAdditionalUsers.Remove(relationship);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Removed additional user {AdditionalUserId} from main user {MainUserId}", 
                    additionalUserId, mainUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing additional user {AdditionalUserId} from main user {MainUserId}", 
                    additionalUserId, mainUserId);
                return false;
            }
        }

        public async Task<IEnumerable<UmiHealth.Domain.Entities.User>> GetAdditionalUsersAsync(Guid mainUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.UserAdditionalUsers
                    .Where(ua => ua.MainUserId == mainUserId)
                    .Include(ua => ua.AdditionalUser)
                    .Select(ua => ua.AdditionalUser)
                    .Where(u => u.IsActive)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting additional users for main user {MainUserId}", mainUserId);
                return Enumerable.Empty<UmiHealth.Domain.Entities.User>();
            }
        }

        public async Task<UmiHealth.Domain.Entities.User?> GetMainUserAsync(Guid additionalUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                var relationship = await _context.UserAdditionalUsers
                    .Include(ua => ua.MainUser)
                    .FirstOrDefaultAsync(ua => ua.AdditionalUserId == additionalUserId, cancellationToken);

                return relationship?.MainUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting main user for additional user {AdditionalUserId}", additionalUserId);
                return null;
            }
        }

        public async Task<bool> CanAccessUserDataAsync(Guid requestingUserId, Guid targetUserId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Users can always access their own data
                if (requestingUserId == targetUserId)
                    return true;

                // Check if requesting user is an additional user of the target user
                var hasAccess = await _context.UserAdditionalUsers
                    .AnyAsync(ua => ua.AdditionalUserId == requestingUserId && ua.MainUserId == targetUserId, cancellationToken);

                return hasAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access permission for user {RequestingUserId} to user {TargetUserId}", 
                    requestingUserId, targetUserId);
                return false;
            }
        }
    }
}
