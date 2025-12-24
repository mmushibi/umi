using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace UmiHealth.Api.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        public AuditLoggingMiddleware(
            RequestDelegate next,
            ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString();
            
            // Add request ID to context for correlation
            context.Items["RequestId"] = requestId;
            
            // Store original response body stream
            var originalResponseBody = context.Response.Body;
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Read request body for logging (only for specific content types)
            string requestBody = string.Empty;
            if (ShouldLogRequestBody(context))
            {
                requestBody = await ReadRequestBodyAsync(context.Request);
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await LogRequestAsync(context, requestId, requestBody, stopwatch.Elapsed, ex);
                throw;
            }

            stopwatch.Stop();

            // Read response body for logging
            var responseBodyContent = await ReadResponseBodyAsync(context.Response);
            
            // Copy response back to original stream
            await responseBody.CopyToAsync(originalResponseBody);

            await LogRequestAsync(context, requestId, requestBody, stopwatch.Elapsed, null, responseBodyContent);
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            request.Body.Position = 0;
            
            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;
            
            return body;
        }

        private async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            response.Body.Position = 0;
            
            using var reader = new StreamReader(response.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            response.Body.Position = 0;
            
            return body;
        }

        private async Task LogRequestAsync(
            HttpContext context, 
            string requestId, 
            string requestBody, 
            TimeSpan duration, 
            Exception? exception = null,
            string responseBody = "")
        {
            try
            {
                var auditLog = new
                {
                    RequestId = requestId,
                    Timestamp = DateTime.UtcNow,
                    Duration = duration.TotalMilliseconds,
                    Request = new
                    {
                        Method = context.Request.Method,
                        Path = context.Request.Path,
                        QueryString = context.Request.QueryString.ToString(),
                        Headers = SanitizeHeaders(context.Request.Headers),
                        Body = ShouldLogRequestBody(context) ? SanitizeBody(requestBody) : "[SKIPPED]"
                    },
                    Response = new
                    {
                        StatusCode = context.Response.StatusCode,
                        Headers = SanitizeHeaders(context.Response.Headers),
                        Body = ShouldLogResponseBody(context) ? SanitizeBody(responseBody) : "[SKIPPED]"
                    },
                    User = new
                    {
                        UserId = context.Items["UserId"]?.ToString(),
                        TenantId = context.Items["TenantId"]?.ToString(),
                        BranchId = context.User.FindFirst("branch_id")?.Value,
                        Role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                    },
                    Client = new
                    {
                        IpAddress = GetClientIpAddress(context),
                        UserAgent = context.Request.Headers["User-Agent"].ToString()
                    },
                    Exception = exception != null ? new
                    {
                        Type = exception.GetType().Name,
                        Message = exception.Message,
                        StackTrace = exception.StackTrace
                    } : null
                };

                if (context.Response.StatusCode >= 400 || exception != null)
                {
                    _logger.LogWarning("API Request Failed: {@AuditLog}", auditLog);
                }
                else
                {
                    _logger.LogInformation("API Request: {@AuditLog}", auditLog);
                }

                // Store critical audit events separately
                if (IsCriticalOperation(context))
                {
                    _logger.LogWarning("CRITICAL AUDIT: {@AuditLog}", auditLog);
                }
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to log audit information for request: {RequestId}", requestId);
            }
        }

        private bool ShouldLogRequestBody(HttpContext context)
        {
            var contentType = context.Request.ContentType?.ToLower() ?? string.Empty;
            var method = context.Request.Method.ToUpper();
            
            // Only log body for specific content types and methods
            var allowedContentTypes = new[] { "application/json", "application/x-www-form-urlencoded" };
            var allowedMethods = new[] { "POST", "PUT", "PATCH" };
            
            return allowedContentTypes.Any(ct => contentType.Contains(ct)) && 
                   allowedMethods.Contains(method) &&
                   !context.Request.Path.StartsWith("/api/v1/auth/login"); // Skip logging passwords
        }

        private bool ShouldLogResponseBody(HttpContext context)
        {
            var contentType = context.Response.ContentType?.ToLower() ?? string.Empty;
            
            // Only log responses with reasonable content types and sizes
            var allowedContentTypes = new[] { "application/json" };
            
            return allowedContentTypes.Any(ct => contentType.Contains(ct)) &&
                   context.Response.ContentLength < 1024 * 10; // Less than 10KB
        }

        private object SanitizeHeaders(IHeaderDictionary headers)
        {
            var sanitized = new Dictionary<string, string>();
            
            foreach (var header in headers)
            {
                var key = header.Key.ToLower();
                
                // Skip sensitive headers
                if (key == "authorization" || key == "cookie" || key == "x-api-key")
                {
                    sanitized[header.Key] = "[REDACTED]";
                }
                else
                {
                    sanitized[header.Key] = string.Join(", ", header.Value);
                }
            }
            
            return sanitized;
        }

        private string SanitizeBody(string body)
        {
            if (string.IsNullOrEmpty(body))
                return body;

            try
            {
                var jsonDoc = JsonDocument.Parse(body);
                var root = jsonDoc.RootElement;
                
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var sanitized = new Dictionary<string, object>();
                    
                    foreach (var property in root.EnumerateObject())
                    {
                        var propertyName = property.Name.ToLower();
                        
                        // Redact sensitive fields
                        if (propertyName.Contains("password") || 
                            propertyName.Contains("token") || 
                            propertyName.Contains("secret") ||
                            propertyName.Contains("key"))
                        {
                            sanitized[property.Name] = "[REDACTED]";
                        }
                        else
                        {
                            sanitized[property.Name] = property.Value;
                        }
                    }
                    
                    return JsonSerializer.Serialize(sanitized);
                }
            }
            catch
            {
                // If JSON parsing fails, return original body
            }
            
            return body;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            // Check for forwarded headers
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault()?.Split(',')[0]?.Trim();
            }
            else if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                ipAddress = realIp.FirstOrDefault();
            }
            
            return ipAddress ?? "Unknown";
        }

        private bool IsCriticalOperation(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
            var method = context.Request.Method.ToUpper();
            
            // Define critical operations that require special audit attention
            var criticalPaths = new[]
            {
                "/api/v1/auth/login",
                "/api/v1/users",
                "/api/v1/tenants",
                "/api/v1/subscriptions",
                "/api/v1/payments",
                "/api/v1/sales"
            };
            
            var criticalMethods = new[] { "POST", "PUT", "DELETE", "PATCH" };
            
            return criticalPaths.Any(p => path.StartsWith(p)) && criticalMethods.Contains(method);
        }
    }
}
