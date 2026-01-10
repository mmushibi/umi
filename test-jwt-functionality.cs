using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.IO;

/// <summary>
/// JWT functionality test using real configuration from appsettings.json
/// </summary>
public class JwtFunctionalityTest
{
    public static void TestJwtFunctionality()
    {
        Console.WriteLine("=== JWT Functionality Test with Real Configuration ===");
        
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        // Get JWT settings from configuration
        var jwtKey = configuration["Jwt:Key"] ?? configuration["JWT_SECRET"];
        var jwtIssuer = configuration["Jwt:Issuer"] ?? configuration["JWT_ISSUER"] ?? "UmiHealth";
        var jwtAudience = configuration["Jwt:Audience"] ?? configuration["JWT_AUDIENCE"] ?? "UmiHealthUsers";
        
        if (string.IsNullOrEmpty(jwtKey) || jwtKey == "CHANGE_ME_TO_SECURE_RANDOM_STRING_MIN_32_CHARS")
        {
            Console.WriteLine("⚠️  Warning: Using default JWT key. Please set JWT_SECRET environment variable or update appsettings.json");
            jwtKey = "umi_health_jwt_secret_key_2024_very_long_and_secure_default_for_development";
        }
        
        Console.WriteLine($"Using JWT configuration:");
        Console.WriteLine($"  Issuer: {jwtIssuer}");
        Console.WriteLine($"  Audience: {jwtAudience}");
        Console.WriteLine($"  Key Length: {jwtKey.Length} characters");
        
        TestJwtConfiguration(jwtKey, jwtIssuer, jwtAudience);
    }
    
    private static void TestJwtConfiguration(string secret, string issuer, string audience)
    {
        try
        {
            var key = Encoding.ASCII.GetBytes(secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Create realistic test claims
            var testUserId = Guid.NewGuid().ToString();
            var testTenantId = Guid.NewGuid().ToString();
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, testUserId),
                new Claim(ClaimTypes.Email, "admin@umihealth.com"),
                new Claim(ClaimTypes.Name, "System Administrator"),
                new Claim("tenant_id", testTenantId),
                new Claim("branch_id", Guid.NewGuid().ToString()),
                new Claim("role", "Admin"),
                new Claim("permissions", "admin:read,admin:write,users:manage"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };
            
            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(15),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            
            // Generate token
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            
            Console.WriteLine($"✓ Token generated successfully");
            Console.WriteLine($"✓ Token length: {tokenString.Length} characters");
            
            // Validate token
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            var principal = tokenHandler.ValidateToken(tokenString, validationParameters, out SecurityToken validatedToken);
            
            Console.WriteLine($"✓ Token validated successfully");
            Console.WriteLine($"✓ User ID: {principal.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
            Console.WriteLine($"✓ Email: {principal.FindFirst(ClaimTypes.Email)?.Value}");
            Console.WriteLine($"✓ Tenant ID: {principal.FindFirst("tenant_id")?.Value}");
            
            // Test token expiration
            Console.WriteLine($"✓ Token expires: {validatedToken.ValidTo}");
            Console.WriteLine($"✓ Time until expiry: {validatedToken.ValidTo - DateTime.UtcNow}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Entry point for running the JWT test
    /// </summary>
    public static void Main()
    {
        // Run the test
        Console.WriteLine("Starting JWT functionality test...");
        TestJwtFunctionality();
        Console.WriteLine("\nJWT functionality test completed.");
    }
}
