using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Text;
using UmiHealth.Application.Authorization;
using UmiHealth.Application.Services;
using UmiHealth.Api.Middleware;

namespace UmiHealth.Api.Configuration
{
    public static class AuthenticationConfiguration
    {
        public static IServiceCollection AddUmiAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            // Add JWT authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    // We'll use the public key for validation
                    // The private key is used only for signing
                    IssuerSigningKey = new RsaSecurityKey(CreateRsaPublicKey(configuration))
                };

                // Events for better error handling
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Additional validation can be added here
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new 
                        { 
                            success = false, 
                            message = "Authentication required" 
                        }));
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new 
                        { 
                            success = false, 
                            message = "Access denied" 
                        }));
                    }
                };
            });

            // Register JWT service
            services.AddSingleton<IJwtTokenService, JwtTokenService>();

            // Register authorization services
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<IBranchAccessService, BranchAccessService>();

            // Add authorization policies
            services.AddUmiAuthorizationPolicies();

            return services;
        }

        public static IApplicationBuilder UseUmiAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseBranchFilter();

            return app;
        }

        private static System.Security.Cryptography.RSA CreateRsaPublicKey(IConfiguration configuration)
        {
            var rsa = System.Security.Cryptography.RSA.Create();
            
            // Try to load public key from configuration
            var publicKeyPem = configuration["Jwt:PublicKeyPem"];
            if (!string.IsNullOrEmpty(publicKeyPem))
            {
                try
                {
                    rsa.ImportFromPem(publicKeyPem);
                    return rsa;
                }
                catch
                {
                    // If loading fails, create new key pair
                }
            }

            // For development, create a new key pair
            // In production, you should load the keys from secure storage
            rsa.KeySize = 2048;
            
            // Export keys for storage (in production, store these securely)
            var privateKeyPem = rsa.ExportRSAPrivateKeyPem();
            var publicKeyPemGenerated = rsa.ExportRSAPublicKeyPem();
            
            // Log the keys for development (REMOVE IN PRODUCTION)
            Console.WriteLine("=== JWT Keys (Development Only) ===");
            Console.WriteLine($"Public Key: {publicKeyPemGenerated}");
            Console.WriteLine($"Private Key: {privateKeyPem}");
            Console.WriteLine("=====================================");
            
            return rsa;
        }
    }
}
