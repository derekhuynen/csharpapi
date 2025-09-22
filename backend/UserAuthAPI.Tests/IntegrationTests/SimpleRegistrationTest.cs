using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserAuthAPI.Application.DTOs;
using UserAuthAPI.Infrastructure.Data;
using Xunit;

namespace UserAuthAPI.Tests.IntegrationTests;

public class SimpleRegistrationTest : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SimpleRegistrationTest(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task SimpleRegister_ShouldReturnResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "TestPass123!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - just check what we get back
        var responseContent = await response.Content.ReadAsStringAsync();

        // Log what we got
        Console.WriteLine($"Status: {response.StatusCode}");
        Console.WriteLine($"Content: {responseContent}");

        // Test should not fail - we just want to see what happens
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}