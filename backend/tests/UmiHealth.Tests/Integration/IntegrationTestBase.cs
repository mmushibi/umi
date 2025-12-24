using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using UmiHealth.Api;
using UmiHealth.Infrastructure.Data;

namespace UmiHealth.Tests.Integration
{
    /// <summary>
    /// Base class for API integration tests
    /// </summary>
    public class IntegrationTestBase : IAsyncLifetime
    {
        protected readonly WebApplicationFactory<Program> Factory;
        protected HttpClient Client;
        protected string BaseUrl = "http://localhost";

        public IntegrationTestBase()
        {
            Factory = new WebApplicationFactory<Program>();
            Client = Factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            // Seed test data
            await SeedTestDataAsync();
        }

        public async Task DisposeAsync()
        {
            // Cleanup
            Client?.Dispose();
            Factory?.Dispose();
        }

        protected virtual async Task SeedTestDataAsync()
        {
            // Override in derived classes to seed test data
            await Task.CompletedTask;
        }

        protected async Task<T> GetAsync<T>(string endpoint, string token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{endpoint}");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        protected async Task<T> PostAsync<T>(string endpoint, object data, string token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{endpoint}")
            {
                Content = JsonContent.Create(data)
            };

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        protected async Task<T> PutAsync<T>(string endpoint, object data, string token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}{endpoint}")
            {
                Content = JsonContent.Create(data)
            };

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        protected async Task DeleteAsync(string endpoint, string token = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}{endpoint}");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"Bearer {token}");
            }

            var response = await Client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }

    /// <summary>
    /// Authentication API integration tests
    /// </summary>
    public class AuthenticationIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var loginRequest = new
            {
                email = "admin@umihealth.com",
                password = "ValidPassword123!",
                tenantSubdomain = "umihealth"
            };

            // Act
            var response = await PostAsync<dynamic>("/api/v1/auth/login", loginRequest);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new
            {
                email = "admin@umihealth.com",
                password = "WrongPassword",
                tenantSubdomain = "umihealth"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HttpRequestException>(
                () => PostAsync<dynamic>("/api/v1/auth/login", loginRequest));
        }

        [Fact]
        public async Task Register_WithValidData_CreatesNewUser()
        {
            // Arrange
            var registerRequest = new
            {
                email = "newuser@umihealth.com",
                password = "NewPassword123!",
                firstName = "John",
                lastName = "Doe",
                tenantSubdomain = "umihealth"
            };

            // Act
            var response = await PostAsync<dynamic>("/api/v1/auth/register", registerRequest);

            // Assert
            Assert.NotNull(response);
        }
    }

    /// <summary>
    /// Inventory Management API integration tests
    /// </summary>
    public class InventoryIntegrationTests : IntegrationTestBase
    {
        private string _authToken;

        protected override async Task SeedTestDataAsync()
        {
            // Login to get token
            var loginRequest = new
            {
                email = "admin@umihealth.com",
                password = "ValidPassword123!",
                tenantSubdomain = "umihealth"
            };

            try
            {
                var response = await PostAsync<dynamic>("/api/v1/auth/login", loginRequest);
                _authToken = response.GetProperty("data").GetProperty("token").GetString();
            }
            catch
            {
                // Use default token if login fails
                _authToken = "test-token";
            }
        }

        [Fact]
        public async Task GetInventory_ReturnsAllProducts()
        {
            // Act
            var response = await GetAsync<dynamic>("/api/v1/inventory", _authToken);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetInventoryByBranch_ReturnsCorrectData()
        {
            // Arrange
            var branchId = Guid.NewGuid();

            // Act
            var response = await GetAsync<dynamic>($"/api/v1/inventory/branch/{branchId}", _authToken);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task UpdateStock_WithValidData_UpdatesSuccessfully()
        {
            // Arrange
            var updateRequest = new
            {
                productId = Guid.NewGuid(),
                quantity = 100,
                branchId = Guid.NewGuid()
            };

            // Act
            var response = await PutAsync<dynamic>("/api/v1/inventory/update-stock", updateRequest, _authToken);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task ReserveInventory_WithSufficientStock_ReserveSuccessfully()
        {
            // Arrange
            var reserveRequest = new
            {
                productId = Guid.NewGuid(),
                quantity = 10,
                branchId = Guid.NewGuid()
            };

            // Act
            var response = await PostAsync<dynamic>("/api/v1/inventory/reserve", reserveRequest, _authToken);

            // Assert
            Assert.NotNull(response);
        }
    }

    /// <summary>
    /// Point of Sale API integration tests
    /// </summary>
    public class PointOfSaleIntegrationTests : IntegrationTestBase
    {
        private string _authToken;

        protected override async Task SeedTestDataAsync()
        {
            var loginRequest = new
            {
                email = "cashier@umihealth.com",
                password = "ValidPassword123!",
                tenantSubdomain = "umihealth"
            };

            try
            {
                var response = await PostAsync<dynamic>("/api/v1/auth/login", loginRequest);
                _authToken = response.GetProperty("data").GetProperty("token").GetString();
            }
            catch
            {
                _authToken = "test-token";
            }
        }

        [Fact]
        public async Task CreateSale_WithValidData_ReturnsSaleId()
        {
            // Arrange
            var saleRequest = new
            {
                saleNumber = $"SAL-{DateTime.UtcNow.Ticks}",
                patientId = Guid.NewGuid(),
                branchId = Guid.NewGuid(),
                items = new[]
                {
                    new { productId = Guid.NewGuid(), quantity = 2, unitPrice = 500 }
                }
            };

            // Act
            var response = await PostAsync<dynamic>("/api/v1/pos/sales", saleRequest, _authToken);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task ProcessPayment_WithValidData_ProcessesSuccessfully()
        {
            // Arrange
            var paymentRequest = new
            {
                saleId = Guid.NewGuid(),
                paymentMethod = "Cash",
                amount = 5000
            };

            // Act
            var response = await PostAsync<dynamic>("/api/v1/pos/payments", paymentRequest, _authToken);

            // Assert
            Assert.NotNull(response);
        }

        [Fact]
        public async Task GetReceipt_WithValidSaleId_ReturnsReceiptData()
        {
            // Arrange
            var saleId = Guid.NewGuid();

            // Act
            var response = await GetAsync<dynamic>($"/api/v1/pos/receipts/{saleId}", _authToken);

            // Assert
            Assert.NotNull(response);
        }
    }
}
