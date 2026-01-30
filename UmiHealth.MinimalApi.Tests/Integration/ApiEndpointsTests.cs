using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using UmiHealth.MinimalApi.Data;
using UmiHealth.MinimalApi.Models;
using UmiHealth.MinimalApi.Tests.TestHelpers;
using Xunit;

namespace UmiHealth.MinimalApi.Tests.Integration
{
    public class ApiEndpointsTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ApiEndpointsTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetDashboardSummary_ReturnsUnauthorized_WithoutToken()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/dashboard/summary");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetDashboardSummary_ReturnsOk_WithValidToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/dashboard/summary");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPatients_ReturnsOk_WithValidToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/patients");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task CreatePatient_ReturnsCreated_WithValidData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newPatient = new
            {
                FirstName = "Test",
                LastName = "Patient",
                Email = "test.patient@email.com",
                PhoneNumber = "555-123-4567",
                Gender = "Female",
                Address = "456 Test Ave",
                BloodType = "A+",
                Allergies = "None",
                MedicalHistory = "No significant history"
            };

            var json = JsonSerializer.Serialize(newPatient);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/patients", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }

        [Fact]
        public async Task GetInventory_ReturnsOk_WithValidToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/inventory");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task CreateInventory_ReturnsCreated_WithValidData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newInventoryItem = new
            {
                ProductName = "Test Medication",
                GenericName = "Test Generic",
                Category = "Test Category",
                ProductCode = "TEST001",
                Barcode = "9876543210",
                Unit = "tablets",
                Quantity = 50,
                ReorderLevel = 10,
                UnitPrice = 9.99m,
                Manufacturer = "Test Manufacturer",
                Supplier = "Test Supplier",
                ExpiryDate = "2025-06-30",
                Description = "Test medication description"
            };

            var json = JsonSerializer.Serialize(newInventoryItem);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/inventory", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }

        [Fact]
        public async Task GetSales_ReturnsOk_WithValidToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateCashierToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/sales");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task CreateSale_ReturnsCreated_WithValidData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateCashierToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newSale = new
            {
                PatientId = "patient-1",
                PaymentMethod = "cash",
                Items = new[]
                {
                    new
                    {
                        InventoryId = "inventory-1",
                        Quantity = 2,
                        UnitPrice = 5.99m
                    }
                }
            };

            var json = JsonSerializer.Serialize(newSale);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/sales", content);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseContent);
        }

        [Fact]
        public async Task GetPrescriptions_ReturnsOk_WithValidToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/prescriptions");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task SearchPatients_ReturnsOk_WithValidToken()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = TestJwtTokenGenerator.GenerateAdminToken();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await client.GetAsync("/api/v1/search/patients?query=John");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }
    }
}
