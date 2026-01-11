using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UmiHealth.Core.Interfaces;

namespace UmiHealth.Core.Interfaces
{
    public interface IUserService
    {
        Task<IPagedResult<IUserDto>> GetUsersAsync(Guid tenantId, int page = 1, int limit = 50, string? search = null, string? role = null, CancellationToken cancellationToken = default);
        Task<IUserDto?> GetUserByIdAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
        Task<IUserDto> CreateUserAsync(Guid tenantId, CreateUserRequest request, CancellationToken cancellationToken = default);
        Task<IUserDto> UpdateUserAsync(Guid userId, Guid tenantId, UpdateUserRequest request, CancellationToken cancellationToken = default);
        Task<bool> DeleteUserAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
        Task<bool> UpdateUserStatusAsync(Guid userId, Guid tenantId, string status, CancellationToken cancellationToken = default);
        Task<IEnumerable<IUserDto>> GetUsersByRoleAsync(Guid tenantId, string role, CancellationToken cancellationToken = default);
        Task<bool> IsEmailUniqueAsync(Guid tenantId, string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<int> GetUsersCountAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }

    public class CreateUserRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public Guid? BranchId { get; set; }
        public bool SendInviteEmail { get; set; } = true;
    }

    public class UpdateUserRequest
    {
        public string Name { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? BranchId { get; set; }
    }
}
