using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using UserAuthAPI.Api;
using UserAuthAPI.Application.DTOs;
using UserAuthAPI.Infrastructure.Data;
using Xunit;

namespace UserAuthAPI.Tests.IntegrationTests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core related services
            var efServiceTypes = new[]
            {
                typeof(DbContextOptions<ApplicationDbContext>),
                typeof(ApplicationDbContext),
                typeof(DbContextOptions)
            };

            foreach (var serviceType in efServiceTypes)
            {
                var descriptorsToRemove = services.Where(d => d.ServiceType == serviceType).ToList();
                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }
            }

            // Add InMemory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
            });
        });

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Add test configuration
            var testConfiguration = new Dictionary<string, string>
            {
                ["Jwt:SecretKey"] = "test-secret-key-for-unit-tests-that-is-at-least-32-characters-long",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:AccessTokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            };

            config.AddInMemoryCollection(testConfiguration!);
        });

        builder.ConfigureLogging(logging =>
        {
            logging.Services.Clear();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
    }
}

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                // Remove the real database context
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add InMemory database for testing
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                });
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnSuccess_WhenValidDataProvided()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "integrationtest@example.com",
            Password = "StrongPass123!",
            FirstName = "Integration",
            LastName = "Test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Register failed with status {response.StatusCode}: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.User.Email.Should().Be(request.Email);
        authResponse.User.FirstName.Should().Be(request.FirstName);
        authResponse.User.LastName.Should().Be(request.LastName);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenInvalidDataProvided()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FirstName = "",
            LastName = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ShouldReturnSuccess_WhenValidCredentialsProvided()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "logintest@example.com",
            Password = "StrongPass123!",
            FirstName = "Login",
            LastName = "Test"
        };

        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.User.Email.Should().Be(loginRequest.Email);
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenInvalidCredentialsProvided()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUnauthorized_WhenNoTokenProvided()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnSuccess_WhenValidTokenProvided()
    {
        // Arrange - Register and login to get a token
        var registerRequest = new RegisterRequest
        {
            Email = "currentuser@example.com",
            Password = "StrongPass123!",
            FirstName = "Current",
            LastName = "User"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(registerContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Add authorization header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var userResponse = JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userResponse.Should().NotBeNull();
        userResponse!.Email.Should().Be(registerRequest.Email);
        userResponse.FirstName.Should().Be(registerRequest.FirstName);
        userResponse.LastName.Should().Be(registerRequest.LastName);
    }

    [Fact]
    public async Task CompleteAuthenticationFlow_ShouldWork()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = "fullflow@example.com",
            Password = "StrongPass123!",
            FirstName = "Full",
            LastName = "Flow"
        };

        // Act & Assert - Register
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var registerAuthResponse = JsonSerializer.Deserialize<AuthResponse>(registerContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        registerAuthResponse.Should().NotBeNull();
        registerAuthResponse!.AccessToken.Should().NotBeNullOrEmpty();

        // Act & Assert - Login
        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var loginAuthResponse = JsonSerializer.Deserialize<AuthResponse>(loginContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        loginAuthResponse.Should().NotBeNull();
        loginAuthResponse!.AccessToken.Should().NotBeNullOrEmpty();
        loginAuthResponse.AccessToken.Should().NotBe(registerAuthResponse.AccessToken); // Should be different tokens

        // Act & Assert - Access protected endpoint
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginAuthResponse.AccessToken);

        var meResponse = await _client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meContent = await meResponse.Content.ReadAsStringAsync();
        var userDto = JsonSerializer.Deserialize<UserDto>(meContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be(registerRequest.Email);
    }
}