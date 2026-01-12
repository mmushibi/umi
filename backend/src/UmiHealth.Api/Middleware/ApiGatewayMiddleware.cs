using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UmiHealth.API.Services;

namespace UmiHealth.API.Middleware
{
    public class ApiGatewayMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiGatewayMiddleware> _logger;
        private readonly IApiGatewayService _gatewayService;

        public ApiGatewayMiddleware(
            RequestDelegate next,
            ILogger<ApiGatewayMiddleware> logger,
            IApiGatewayService gatewayService)
        {
            _next = next;
            _logger = logger;
            _gatewayService = gatewayService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip gateway routing for local endpoints
            if (ShouldSkipGatewayRouting(context.Request.Path))
            {
                await _next(context);
                return;
            }

            try
            {
                var gatewayRequest = await CreateGatewayRequestAsync(context);
                var gatewayResponse = await _gatewayService.RouteRequestAsync(gatewayRequest);

                await WriteGatewayResponseAsync(context, gatewayResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway routing failed for request: {RequestPath}", context.Request.Path);
                
                context.Response.StatusCode = 502;
                await context.Response.WriteAsync("Gateway routing failed");
            }
        }

        private bool ShouldSkipGatewayRouting(string path)
        {
            var skipPaths = new[]
            {
                "/health",
                "/api/v1/auth",
                "/api/v1/gateway",
                "/swagger",
                "/swagger-ui",
                "/api-docs"
            };

            foreach (var skipPath in skipPaths)
            {
                if (path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<GatewayRequest> CreateGatewayRequestAsync(HttpContext context)
        {
            var request = context.Request;
            
            // Read request body
            string requestBody = string.Empty;
            if (request.ContentLength > 0 && 
                (request.Method == "POST" || request.Method == "PUT" || request.Method == "PATCH"))
            {
                request.EnableBuffering();
                request.Body.Position = 0;
                
                using var reader = new StreamReader(request.Body, leaveOpen: true);
                requestBody = await reader.ReadToEndAsync();
                request.Body.Position = 0;
            }

            // Copy headers
            var headers = new Dictionary<string, string>();
            foreach (var header in request.Headers)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            return new GatewayRequest
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                Headers = headers,
                Body = requestBody,
                ContentType = request.ContentType ?? "application/json",
                RequestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString()
            };
        }

        private async Task WriteGatewayResponseAsync(HttpContext context, GatewayResponse gatewayResponse)
        {
            context.Response.StatusCode = gatewayResponse.StatusCode;
            
            // Copy headers
            foreach (var header in gatewayResponse.Headers)
            {
                if (!context.Response.Headers.ContainsKey(header.Key))
                {
                    context.Response.Headers[header.Key] = header.Value;
                }
            }

            // Add gateway-specific headers
            context.Response.Headers["X-Gateway-Response-Time"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (!string.IsNullOrEmpty(gatewayResponse.Content))
            {
                await context.Response.WriteAsync(gatewayResponse.Content);
            }
        }
    }
}
