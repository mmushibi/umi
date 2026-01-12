using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using UmiHealth.API.Services;

namespace UmiHealth.API.Middleware
{
    /// <summary>
    /// Middleware to audit security events and enforce IP blocking
    /// </summary>
    public class SecurityAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ILogger<SecurityAuditMiddleware> _logger;

        public SecurityAuditMiddleware(
            RequestDelegate next,
            ISecurityAuditService securityAuditService,
            ILogger<SecurityAuditMiddleware> logger)
        {
            _next = next;
            _securityAuditService = securityAuditService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var clientIp = GetClientIpAddress(context);
            
            // Check if IP is blocked
            var isBlocked = await _securityAuditService.IsIpAddressBlockedAsync(clientIp);
            if (isBlocked)
            {
                _logger.LogWarning("Blocked IP address attempted access: {IpAddress}", clientIp);
                
                // Log blocked access attempt
                await _securityAuditService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.UnauthorizedAccess,
                    Description = "Blocked IP attempted access",
                    IpAddress = clientIp,
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestPath = context.Request.Path,
                    RiskLevel = SecurityRiskLevel.High,
                    Metadata = new Dictionary<string, object>
                    {
                        ["StatusCode"] = 403,
                        ["Reason"] = "IP Blocked"
                    }
                });

                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Access denied. Your IP address has been blocked.");
                return;
            }

            // Log request start
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var requestPath = context.Request.Path;
            var method = context.Request.Method;

            // Log suspicious patterns
            if (IsSuspiciousRequest(context))
            {
                await _securityAuditService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.SuspiciousActivity,
                    Description = $"Suspicious request pattern detected: {method} {requestPath}",
                    IpAddress = clientIp,
                    UserId = userId,
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestPath = requestPath,
                    RiskLevel = SecurityRiskLevel.Medium,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Method"] = method,
                        ["QueryString"] = context.Request.QueryString.ToString(),
                        ["Headers"] = GetRelevantHeaders(context)
                    }
                });
            }

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log security-related exceptions
                await _securityAuditService.LogSecurityEventAsync(new SecurityEvent
                {
                    EventType = SecurityEventType.SecurityViolation,
                    Description = $"Exception during request processing: {ex.Message}",
                    IpAddress = clientIp,
                    UserId = userId,
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    RequestPath = requestPath,
                    RiskLevel = SecurityRiskLevel.High,
                    Metadata = new Dictionary<string, object>
                    {
                        ["ExceptionType"] = ex.GetType().Name,
                        ["ExceptionMessage"] = ex.Message,
                        ["StackTrace"] = ex.StackTrace
                    }
                });

                throw;
            }
            finally
            {
                stopwatch.Stop();
                
                // Log long-running requests as potential DoS
                if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds
                {
                    await _securityAuditService.LogSecurityEventAsync(new SecurityEvent
                    {
                        EventType = SecurityEventType.RateLimitExceeded,
                        Description = $"Long-running request detected: {stopwatch.ElapsedMilliseconds}ms",
                        IpAddress = clientIp,
                        UserId = userId,
                        UserAgent = context.Request.Headers["User-Agent"].ToString(),
                        RequestPath = requestPath,
                        RiskLevel = SecurityRiskLevel.Low,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Duration"] = stopwatch.ElapsedMilliseconds,
                            ["Method"] = method
                        }
                    });
                }
            }
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xForwardedFor))
            {
                return xForwardedFor.Split(',')[0].Trim();
            }

            var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(xRealIp))
            {
                return xRealIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsSuspiciousRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";
            var queryString = context.Request.QueryString.Value?.ToLower() ?? "";
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();

            // Check for common attack patterns
            var suspiciousPatterns = new[]
            {
                "script", "alert", "onload", "onerror", // XSS
                "union", "select", "drop", "insert", "update", "delete", // SQL Injection
                "../", "..\\", // Path traversal
                "<script", "</script>", // XSS tags
                "exec(", "system(", "eval(" // Code execution
            };

            foreach (var pattern in suspiciousPatterns)
            {
                if (path.Contains(pattern) || queryString.Contains(pattern))
                {
                    return true;
                }
            }

            // Check for missing user agent (bot/scanner)
            if (string.IsNullOrEmpty(userAgent) || userAgent == "unknown")
            {
                return true;
            }

            // Check for common scanner user agents
            var scannerAgents = new[]
            {
                "sqlmap", "nikto", "nmap", "burp", "owasp", "scanner",
                "python-requests", "curl", "wget"
            };

            foreach (var agent in scannerAgents)
            {
                if (userAgent.Contains(agent))
                {
                    return true;
                }
            }

            return false;
        }

        private Dictionary<string, object> GetRelevantHeaders(HttpContext context)
        {
            var relevantHeaders = new Dictionary<string, object>();
            var headersToLog = new[] { "Content-Type", "Authorization", "X-Requested-With", "Referer" };

            foreach (var header in headersToLog)
            {
                if (context.Request.Headers.ContainsKey(header))
                {
                    var value = context.Request.Headers[header].ToString();
                    // Mask sensitive headers
                    if (header.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    {
                        value = value.Length > 20 ? value.Substring(0, 20) + "..." : "***";
                    }
                    relevantHeaders[header] = value;
                }
            }

            return relevantHeaders;
        }
    }
}
