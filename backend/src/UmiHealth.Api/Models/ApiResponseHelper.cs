using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UmiHealth.Api.Models
{
    /// <summary>
    /// Helper class for creating standardized API responses
    /// </summary>
    public static class ApiResponseHelper
    {
        /// <summary>
        /// Creates a successful response with data
        /// </summary>
        public static IActionResult Success<T>(
            T data,
            string message = "Request processed successfully",
            int statusCode = StatusCodes.Status200OK,
            HttpContext httpContext = null,
            PaginationInfo pagination = null)
        {
            var response = new ApiResponse<T>
            {
                RequestId = httpContext?.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString(),
                StatusCode = statusCode,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Pagination = pagination
            };

            if (httpContext?.Items.ContainsKey("RequestStopwatch") == true &&
                httpContext.Items["RequestStopwatch"] is Stopwatch sw)
            {
                response.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            }

            return new ObjectResult(response) { StatusCode = statusCode };
        }

        /// <summary>
        /// Creates a successful response without data (for operations like delete)
        /// </summary>
        public static IActionResult SuccessNoContent(
            string message = "Request processed successfully",
            HttpContext httpContext = null)
        {
            return new NoContentResult();
        }

        /// <summary>
        /// Creates a successful response with created data (201)
        /// </summary>
        public static IActionResult Created<T>(
            T data,
            string message = "Resource created successfully",
            string location = null,
            HttpContext httpContext = null)
        {
            var response = new ApiResponse<T>
            {
                RequestId = httpContext?.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString(),
                StatusCode = StatusCodes.Status201Created,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            if (httpContext?.Items.ContainsKey("RequestStopwatch") == true &&
                httpContext.Items["RequestStopwatch"] is Stopwatch sw)
            {
                response.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            }

            var result = new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };

            if (!string.IsNullOrEmpty(location))
            {
                result.ContentLocation = location;
            }

            return result;
        }

        /// <summary>
        /// Creates an error response
        /// </summary>
        public static IActionResult Error(
            string message,
            int statusCode = StatusCodes.Status400BadRequest,
            string status = "BadRequest",
            Dictionary<string, IEnumerable<string>> errors = null,
            HttpContext httpContext = null)
        {
            var response = new ApiErrorResponse
            {
                RequestId = httpContext?.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString(),
                StatusCode = statusCode,
                Status = status,
                Message = message,
                Errors = errors,
                Timestamp = DateTime.UtcNow,
                Path = httpContext?.Request.Path,
                Method = httpContext?.Request.Method
            };

            if (httpContext?.Items.ContainsKey("RequestStopwatch") == true &&
                httpContext.Items["RequestStopwatch"] is Stopwatch sw)
            {
                response.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            }

            return new ObjectResult(response) { StatusCode = statusCode };
        }

        /// <summary>
        /// Creates a validation error response
        /// </summary>
        public static IActionResult ValidationError(
            Dictionary<string, IEnumerable<string>> errors,
            string message = "One or more validation errors occurred",
            HttpContext httpContext = null)
        {
            return Error(
                message,
                StatusCodes.Status400BadRequest,
                "ValidationError",
                errors,
                httpContext);
        }

        /// <summary>
        /// Creates a not found error response
        /// </summary>
        public static IActionResult NotFound(
            string message = "The requested resource was not found",
            HttpContext httpContext = null)
        {
            return Error(
                message,
                StatusCodes.Status404NotFound,
                "NotFound",
                httpContext: httpContext);
        }

        /// <summary>
        /// Creates an unauthorized error response
        /// </summary>
        public static IActionResult Unauthorized(
            string message = "You are not authorized to access this resource",
            HttpContext httpContext = null)
        {
            return Error(
                message,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                httpContext: httpContext);
        }

        /// <summary>
        /// Creates a forbidden error response
        /// </summary>
        public static IActionResult Forbidden(
            string message = "You do not have permission to access this resource",
            HttpContext httpContext = null)
        {
            return Error(
                message,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                httpContext: httpContext);
        }

        /// <summary>
        /// Creates a conflict error response
        /// </summary>
        public static IActionResult Conflict(
            string message = "The request conflicts with the current state of the resource",
            HttpContext httpContext = null)
        {
            return Error(
                message,
                StatusCodes.Status409Conflict,
                "Conflict",
                httpContext: httpContext);
        }

        /// <summary>
        /// Creates a server error response
        /// </summary>
        public static IActionResult ServerError(
            string message = "An unexpected error occurred while processing your request",
            string requestId = null,
            HttpContext httpContext = null)
        {
            var response = new ApiErrorResponse
            {
                RequestId = requestId ?? httpContext?.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString(),
                StatusCode = StatusCodes.Status500InternalServerError,
                Status = "InternalServerError",
                Message = message,
                Timestamp = DateTime.UtcNow,
                Path = httpContext?.Request.Path,
                Method = httpContext?.Request.Method
            };

            if (httpContext?.Items.ContainsKey("RequestStopwatch") == true &&
                httpContext.Items["RequestStopwatch"] is Stopwatch sw)
            {
                response.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            }

            return new ObjectResult(response) { StatusCode = StatusCodes.Status500InternalServerError };
        }

        /// <summary>
        /// Creates a paginated success response
        /// </summary>
        public static IActionResult SuccessPaginated<T>(
            IEnumerable<T> data,
            int pageNumber,
            int pageSize,
            int totalRecords,
            string message = "Request processed successfully",
            HttpContext httpContext = null)
        {
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            
            var pagination = new PaginationInfo
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };

            var response = new ApiResponse<IEnumerable<T>>
            {
                RequestId = httpContext?.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString(),
                StatusCode = StatusCodes.Status200OK,
                Message = message,
                Data = data,
                Timestamp = DateTime.UtcNow,
                Pagination = pagination
            };

            if (httpContext?.Items.ContainsKey("RequestStopwatch") == true &&
                httpContext.Items["RequestStopwatch"] is Stopwatch sw)
            {
                response.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            }

            return new ObjectResult(response) { StatusCode = StatusCodes.Status200OK };
        }
    }
}
