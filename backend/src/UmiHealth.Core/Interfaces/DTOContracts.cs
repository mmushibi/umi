using System;
using System.Collections.Generic;

namespace UmiHealth.Core.Interfaces
{
    public interface IUserDto
    {
        Guid Id { get; }
        string Email { get; }
        string FirstName { get; }
        string LastName { get; }
        string PhoneNumber { get; }
        Guid TenantId { get; }
        string TenantName { get; }
        Guid? BranchId { get; }
        string? BranchName { get; }
        DateTime LastLoginAt { get; }
        bool IsActive { get; }
    }

    public interface IPagedResult<out T>
    {
        IEnumerable<T> Data { get; }
        int TotalCount { get; }
        int Page { get; }
        int PageSize { get; }
        int TotalPages { get; }
        bool HasNextPage { get; }
        bool HasPreviousPage { get; }
    }
}
