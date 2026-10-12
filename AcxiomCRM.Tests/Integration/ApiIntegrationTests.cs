using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AcxiomCRM.DTOs;

namespace AcxiomCRM.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ApiIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Theory]
        [InlineData("/api/customers")]
        [InlineData("/api/leads")]
        [InlineData("/api/opportunities")]
        [InlineData("/api/followups")]
        [InlineData("/api/reports/pipeline")]
        public async Task ProtectedEndpoints_WhenAnonymous_ReturnUnauthorized(string endpoint)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AuthApi_Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var badLogin = new LoginRequestDto
            {
                Email = "nobody@acxiomcrm.com",
                Password = "WrongPassword123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", badLogin);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Invalid credentials", content);
        }

        [Fact]
        public async Task AuthApi_Login_WithMissingFields_ReturnsBadRequest()
        {
            // Arrange
            var emptyLogin = new LoginRequestDto
            {
                Email = "",
                Password = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", emptyLogin);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CompleteApiWorkflow_Login_CreateCustomer_GetCustomer_Reports()
        {
            // 1. Login as Admin
            var loginDto = new LoginRequestDto
            {
                Email = "admin@acxiomcrm.com",
                Password = "Admin@123"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            // 2. Query Customers (authenticated via cookie set in client)
            var getCustResponse = await _client.GetAsync("/api/customers");
            Assert.Equal(HttpStatusCode.OK, getCustResponse.StatusCode);

            // 3. Create a Customer via API
            var newCustomer = new CreateCustomerDto
            {
                CustomerName = "Integration Test Client",
                Email = $"api-test-{Guid.NewGuid():N}@client.com",
                Phone = "9192939495",
                CompanyName = "Integration Corp",
                Status = "Active"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/customers", newCustomer);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdBody = await createResponse.Content.ReadFromJsonAsync<ApiResponse<CustomerDto>>();
            Assert.NotNull(createdBody);
            Assert.True(createdBody.Success);
            Assert.NotNull(createdBody.Data);
            Assert.Equal("Integration Test Client", createdBody.Data.CustomerName);
            var customerId = createdBody.Data.CustomerId;

            // 4. Fetch specific Customer by ID
            var fetchResponse = await _client.GetAsync($"/api/customers/{customerId}");
            Assert.Equal(HttpStatusCode.OK, fetchResponse.StatusCode);

            // 5. Query non-existent Customer returns 404
            var notFoundResponse = await _client.GetAsync("/api/customers/999999");
            Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);

            // 6. Attempt to create invalid Opportunity (Amount <= 0) returns 400
            var invalidOpp = new CreateOpportunityDto
            {
                OpportunityName = "Negative Amount Opp",
                CustomerId = customerId,
                Amount = -1000m,
                Stage = "Qualification",
                Probability = 20,
                ExpectedCloseDate = DateTime.Today.AddDays(15),
                Status = "Open"
            };

            var oppResponse = await _client.PostAsJsonAsync("/api/opportunities", invalidOpp);
            Assert.Equal(HttpStatusCode.BadRequest, oppResponse.StatusCode);

            // 7. Get Pipeline Report via API
            var reportResponse = await _client.GetAsync("/api/reports/pipeline");
            Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);

            // 8. Logout
            var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
            Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        }
    }
}
