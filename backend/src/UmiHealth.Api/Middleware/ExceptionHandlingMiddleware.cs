using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UmiHealth.Api.Models;
using UmiHealth.Core.Exceptions;

namespace UmiHealth.Api.Middleware
{
    /// <summary>
    /// Global exception handling middleware for consistent error response formatting.
    /// Captures all unhandled exceptions and returns standardized error responses.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unhandled exception occurred. RequestId: {RequestId}", requestId);
                await HandleExceptionAsync(context, ex, requestId, stopwatch.ElapsedMilliseconds);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, string requestId, long elapsedMs)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method,
                ElapsedMilliseconds = elapsedMs
            };

            switch (exception)
            {
                // Validation Exceptions
                case ValidationException validationEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Status = "ValidationError";
                    response.Message = "One or more validation errors occurred.";
                    response.Errors = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToList() as IEnumerable<string>
                        );
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                // Tenant Not Found
                case TenantNotFoundException tenantEx:
                    response.StatusCode = StatusCodes.Status404NotFound;
                    response.Status = "NotFound";
                    response.Message = tenantEx.Message;
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    break;

                // Tenant Access Denied
                case TenantAccessDeniedException accessEx:
                    response.StatusCode = StatusCodes.Status403Forbidden;
                    response.Status = "Forbidden";
                    response.Message = accessEx.Message;
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    break;

                // Insufficient Inventory
                case InsufficientInventoryException inventoryEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Status = "InvalidOperation";
                    response.Message = inventoryEx.Message;
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                // Duplicate Entity
                case DuplicateEntityException duplicateEx:
                    response.StatusCode = StatusCodes.Status409Conflict;
                    response.Status = "Conflict";
                    response.Message = duplicateEx.Message;
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    break;

                // Invalid Operation
                case InvalidOperationException invalidOpEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Status = "InvalidOperation";
                    response.Message = invalidOpEx.Message;
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                // Unauthorized Access
                case UnauthorizedAccessException unauthorizedEx:
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.Status = "Unauthorized";
                    response.Message = "You are not authorized to perform this action.";
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    break;

                // Generic Domain Exceptions
                case DomainException domainEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Status = "DomainError";
                    response.Message = domainEx.Message;
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                // Default Server Error
                default:
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Status = "InternalServerError";
                    response.Message = "An unexpected error occurred. Please contact support with the RequestId.";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            return context.Response.WriteAsJsonAsync(response);
        }
    }

    /// <summary>
    /// Custom exception base class for domain-specific exceptions
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }

    /// <summary>
    /// Exception thrown when a tenant is not found
    /// </summary>
    public class TenantNotFoundException : DomainException
    {
        public TenantNotFoundException(string tenantId)
            : base($"Tenant with ID '{tenantId}' not found.") { }
    }

    /// <summary>
    /// Exception thrown when a user does not have access to a tenant
    /// </summary>
    public class TenantAccessDeniedException : DomainException
    {
        public TenantAccessDeniedException(string message)
            : base(message ?? "You do not have access to this tenant.") { }
    }

    /// <summary>
    /// Exception thrown when inventory is insufficient for an operation
    /// </summary>
    public class InsufficientInventoryException : DomainException
    {
        public InsufficientInventoryException(string productName, int requested, int available)
            : base($"Insufficient inventory for '{productName}'. Requested: {requested}, Available: {available}") { }
    }

    /// <summary>
    /// Exception thrown when attempting to create a duplicate entity
    /// </summary>
    public class DuplicateEntityException : DomainException
    {
        public DuplicateEntityException(string entityType, string key)
            : base($"An entity of type '{entityType}' with key '{key}' already exists.") { }
    }
}
