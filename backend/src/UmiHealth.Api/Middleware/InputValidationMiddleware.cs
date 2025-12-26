using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UmiHealth.Api.Middleware
{
    /// <summary>
    /// Middleware for input validation and sanitization
    /// </summary>
    public class InputValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<InputValidationMiddleware> _logger;

        public InputValidationMiddleware(RequestDelegate next, ILogger<InputValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for certain endpoints
            if (ShouldSkipValidation(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Validate request size
            if (context.Request.ContentLength > 10 * 1024 * 1024) // 10MB limit
            {
                _logger.LogWarning("Request size exceeded limit: {ContentLength}", context.Request.ContentLength);
                context.Response.StatusCode = 413; // Payload Too Large
                await context.Response.WriteAsync("Request payload too large");
                return;
            }

            // Validate and sanitize request body for POST/PUT requests
            if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
            {
                var originalBody = context.Request.Body;
                try
                {
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    
                    // Reset the stream for the next middleware
                    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

                    // Validate for potential XSS attacks
                    if (ContainsXssPatterns(body))
                    {
                        _logger.LogWarning("Potential XSS attack detected in request body from {RemoteIpAddress}", 
                            context.Connection.RemoteIpAddress);
                        context.Response.StatusCode = 400; // Bad Request
                        await context.Response.WriteAsync("Invalid input detected");
                        return;
                    }

                    // Validate for SQL injection patterns
                    if (ContainsSqlInjectionPatterns(body))
                    {
                        _logger.LogWarning("Potential SQL injection attack detected in request body from {RemoteIpAddress}", 
                            context.Connection.RemoteIpAddress);
                        context.Response.StatusCode = 400; // Bad Request
                        await context.Response.WriteAsync("Invalid input detected");
                        return;
                    }

                    // Reset body stream again
                    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during input validation");
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid request format");
                    return;
                }
                finally
                {
                    context.Request.Body = originalBody;
                }
            }

            // Validate query parameters
            foreach (var queryParam in context.Request.Query)
            {
                if (ContainsXssPatterns(queryParam.Value) || ContainsSqlInjectionPatterns(queryParam.Value))
                {
                    _logger.LogWarning("Potential attack detected in query parameter '{Param}' from {RemoteIpAddress}", 
                        queryParam.Key, context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid input detected");
                    return;
                }
            }

            await _next(context);
        }

        private bool ShouldSkipValidation(string path)
        {
            // Skip validation for health checks, Swagger, and file uploads
            return path.StartsWithSegments("/health") ||
                   path.StartsWithSegments("/swagger") ||
                   path.StartsWithSegments("/hangfire") ||
                   path.Contains("/upload") ||
                   path.Contains("/export");
        }

        private bool ContainsXssPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var xssPatterns = new[]
            {
                "<script",
                "javascript:",
                "onload=",
                "onerror=",
                "onclick=",
                "onmouseover=",
                "onfocus=",
                "onblur=",
                "onchange=",
                "onsubmit=",
                "eval(",
                "expression(",
                "vbscript:",
                "data:text/html",
                "<iframe",
                "<object",
                "<embed",
                "<link",
                "<meta",
                "<style",
                "@import",
                "alert(",
                "confirm(",
                "prompt("
            };

            var lowerInput = input.ToLowerInvariant();
            return xssPatterns.Any(pattern => lowerInput.Contains(pattern));
        }

        private bool ContainsSqlInjectionPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var sqlPatterns = new[]
            {
                "'",
                "\"",
                ";",
                "--",
                "/*",
                "*/",
                "xp_",
                "sp_",
                "drop ",
                "delete ",
                "truncate ",
                "insert ",
                "update ",
                "union ",
                "select ",
                "exec ",
                "execute ",
                "cast(",
                "convert(",
                "char(",
                "ascii(",
                "substring(",
                "waitfor delay",
                "benchmark(",
                "sleep(",
                "pg_sleep(",
                "1=1",
                "1 = 1",
                "true",
                "null",
                "or 1=1",
                "and 1=1",
                "' or '1'='1",
                "' or 1=1--",
                "admin'--",
                "admin'/*",
                "' or 1=1#",
                "' or 1=1/*",
                "') or '1'='1--",
                "') or ('1'='1--"
            };

            var lowerInput = input.ToLowerInvariant();
            return sqlPatterns.Any(pattern => lowerInput.Contains(pattern));
        }
    }
}
