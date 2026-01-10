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
        /// Indicates if operation was successful
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
}
