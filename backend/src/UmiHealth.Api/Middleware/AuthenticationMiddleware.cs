using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using UmiHealth.Api.Services;

namespace UmiHealth.Api.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly IRsaKeyService _rsaKeyService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;

        public AuthenticationMiddleware(
            RequestDelegate next,
            ILogger<AuthenticationMiddleware> logger,
            IRsaKeyService rsaKeyService,
            ITokenBlacklistService tokenBlacklistService,
            IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _rsaKeyService = rsaKeyService;
            _tokenBlacklistService = tokenBlacklistService;
            _jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            _jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for health checks and auth endpoints
            if (ShouldSkipAuthentication(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var token = ExtractToken(context.Request);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No token provided for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Authentication required");
                return;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                
                // First, decode the token to get the JWT ID for blacklist checking
                var jsonToken = tokenHandler.ReadJwtToken(token);
                var tokenId = jsonToken.Id;
                
                // Check if token is blacklisted
                if (!string.IsNullOrEmpty(tokenId) && await _tokenBlacklistService.IsTokenBlacklistedAsync(tokenId))
                {
                    _logger.LogWarning("Blacklisted token used for request: {RequestPath}", context.Request.Path);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token has been revoked");
                    return;
                }

                var key = new RsaSecurityKey(_rsaKeyService.GetPublicKey());

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
                
                // Add user context to HttpContext
                context.User = principal;
                
                // Add user ID to items for easy access
                var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    context.Items["UserId"] = userId;
                }

                _logger.LogDebug("Authentication successful for user: {UserId}", userId);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token expired for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Token expired");
                return;
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                _logger.LogWarning("Invalid token signature for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token signature");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed for request: {RequestPath}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Authentication failed");
                return;
            }

            await _next(context);
        }

        private bool ShouldSkipAuthentication(string path)
        {
            var skipPaths = new[]
            {
                "/health",
                "/api/v1/auth/login",
                "/api/v1/auth/register",
                "/api/v1/auth/refresh",
                "/swagger",
                "/swagger-ui",
                "/api-docs"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private string? ExtractToken(HttpRequest request)
        {
            // Try Authorization header first
            if (request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authValue = authHeader.FirstOrDefault();
                if (!string.IsNullOrEmpty(authValue) && authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authValue.Substring("Bearer ".Length).Trim();
                }
            }

            // Try query parameter for development/testing
            if (request.Query.TryGetValue("token", out var tokenQuery))
            {
                return tokenQuery.FirstOrDefault();
            }

            return null;
        }
    }
}
