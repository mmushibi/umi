using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace UmiHealth.Api.Configuration
{
    /// <summary>
    /// Standard API response wrapper for consistent response format
    /// </summary>
    /// <typeparam name="T">Type of the data payload</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the operation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message describing the result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Data payload for successful responses
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Error details for failed responses
        /// </summary>
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Request correlation ID for tracing
        /// </summary>
        public string? CorrelationId { get; set; }
    }

    /// <summary>
    /// Paginated response wrapper for list endpoints
    /// </summary>
    /// <typeparam name="T">Type of items in the collection</typeparam>
    public class PaginatedResponse<T>
    {
        /// <summary>
        /// Collection of items
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

        /// <summary>
        /// Indicates if there's a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        /// <summary>
        /// Indicates if there's a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;
    }

    /// <summary>
    /// Authentication request models
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// User email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User password
        /// </summary>
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Optional tenant identifier for multi-tenant login
        /// </summary>
        public string? TenantCode { get; set; }

        /// <summary>
        /// Remember me flag for extended session
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }

    public class RegisterRequest
    {
        /// <summary>
        /// User email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User password
        /// </summary>
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// User first name
        /// </summary>
        [Required]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// User last name
        /// </summary>
        [Required]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// User phone number
        /// </summary>
        [Phone]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Tenant invitation code (if applicable)
        /// </summary>
        public string? InvitationCode { get; set; }
    }

    public class AuthResponse
    {
        /// <summary>
        /// JWT access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// JWT refresh token
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Token expiration time
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// User information
        /// </summary>
        public UserDto User { get; set; } = new();

        /// <summary>
        /// Operation success flag
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response message
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    public class UserDto
    {
        /// <summary>
        /// Unique user identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// User email address
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User phone number
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// User username
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// User first name
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// User last name
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// User role within the system
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Tenant identifier
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Branch identifier
        /// </summary>
        public string? BranchId { get; set; }

        /// <summary>
        /// Branch access permissions
        /// </summary>
        public List<string>? BranchAccess { get; set; }

        /// <summary>
        /// User permissions
        /// </summary>
        public List<string>? Permissions { get; set; }

        /// <summary>
        /// Account active status
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Email verification status
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// Phone verification status
        /// </summary>
        public bool PhoneVerified { get; set; }

        /// <summary>
        /// Two-factor authentication enabled
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLogin { get; set; }
    }
}
