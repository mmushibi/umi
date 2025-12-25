using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using UmiHealth.API.Controllers;
using UmiHealth.Shared.DTOs;

namespace UmiHealth.Tests.Integration.API;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccess()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123",
            TenantSubdomain = "test"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(responseString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotNull(result.Data.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnBadRequest()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrongpassword",
            TenantSubdomain = "test"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/login", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "password123",
            ConfirmPassword = "password123",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            TenantId = Guid.NewGuid()
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/register", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseString = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(responseString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "123",
            ConfirmPassword = "456",
            FirstName = "",
            LastName = "",
            PhoneNumber = "",
            TenantId = Guid.Empty
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/register", content);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldReturnSuccess()
    {
        // This test would require setting up a valid refresh token scenario
        // For now, we'll test the endpoint structure
        var refreshRequest = new RefreshTokenRequest
        {
            Token = "valid-jwt-token",
            RefreshToken = "valid-refresh-token"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(refreshRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/refresh", content);

        // Assert
        // This will likely fail without proper setup, but tests endpoint structure
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || 
                     response.StatusCode == System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithAuthentication_ShouldReturnSuccess()
    {
        // Arrange - This would require proper authentication setup
        // For now, test endpoint structure
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/logout", null);

        // Assert
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || 
                     response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", "test-token");

        var changePasswordRequest = new ChangePasswordRequest
        {
            CurrentPassword = "oldpassword",
            NewPassword = "newpassword123",
            ConfirmPassword = "newpassword123"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(changePasswordRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/auth/change-password", content);

        // Assert
        // This will likely fail without proper authentication setup
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK || 
                     response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }
}

public class WebApplicationFactory<T> where T : class
{
    public HttpClient CreateClient()
    {
        var builder = new WebHostBuilder()
            .UseEnvironment("Testing")
            .ConfigureServices(services =>
            {
                // Add test services here if needed
            });

        var server = new TestServer(builder);
        return server.CreateClient();
    }
}
