using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Simple test to verify JWT functionality after standardization
/// </summary>
public class JwtFunctionalityTest
{
    public static void TestJwtFunctionality()
    {
        Console.WriteLine("=== JWT Functionality Test ===");
        
        // Test Development Configuration
        Console.WriteLine("\n--- Development Configuration ---");
        TestJwtConfiguration(
            "_-e%(@}wO.D%o*%q.#1@J;?$Lu5=r{?)",
            "UmiHealth-Dev",
            "UmiHealthDevUsers"
        );
        
        // Test Production Configuration
        Console.WriteLine("\n--- Production Configuration ---");
        TestJwtConfiguration(
            "RYS^7$dc^$x:d3RNnSLN|%Y9KRrVXS+|.kEATH_z#M_z7p=;^XHy#a1xu]J_VWS[",
            "UmiHealth",
            "UmiHealthUsers"
        );
    }
    
    private static void TestJwtConfiguration(string secret, string issuer, string audience)
    {
        try
        {
            var key = Encoding.ASCII.GetBytes(secret);
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Create test claims
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Email, "test@example.com"),
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim("tenant_id", "test-tenant-id")
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
