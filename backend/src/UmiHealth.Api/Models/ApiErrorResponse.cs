using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UmiHealth.Api.Models
{
    /// <summary>
    /// Standardized API error response model for all error scenarios
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// Unique request identifier for tracking and debugging
        /// </summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Status category (e.g., "ValidationError", "NotFound", "ServerError")
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; }

        /// <summary>
        /// Human-readable error message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Detailed error information (validation errors, nested exceptions, etc.)
        /// </summary>
        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, IEnumerable<string>> Errors { get; set; }

        /// <summary>
        /// Additional error details for debugging
        /// </summary>
        [JsonPropertyName("details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Details { get; set; }

        /// <summary>
        /// Request path that caused the error
        /// </summary>
        [JsonPropertyName("path")]
        public string Path { get; set; }

        /// <summary>
        /// HTTP method that caused the error
        /// </summary>
        [JsonPropertyName("method")]
        public string Method { get; set; }

        /// <summary>
        /// Timestamp of when the error occurred (UTC)
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// How long the request took in milliseconds
        /// </summary>
        [JsonPropertyName("elapsedMilliseconds")]
        public long ElapsedMilliseconds { get; set; }
    }

    /// <summary>
    /// Standardized success response model for all API endpoints
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Unique request identifier for tracking
        /// </summary>
        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// HTTP status code
        /// </summary>
        [JsonPropertyName("statusCode")]
        public int StatusCode { get; set; }

        /// <summary>
        /// Success message
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; }

        /// <summary>
        /// Response data payload
        /// </summary>
        [JsonPropertyName("data")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T Data { get; set; }

        /// <summary>
        /// Timestamp of the response (UTC)
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// How long the request took in milliseconds
        /// </summary>
        [JsonPropertyName("elapsedMilliseconds")]
        public long ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Pagination information if applicable
        /// </summary>
        [JsonPropertyName("pagination")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationInfo Pagination { get; set; }
    }

    /// <summary>
    /// Pagination metadata for list responses
    /// </summary>
    public class PaginationInfo
    {
        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }
    }
}
