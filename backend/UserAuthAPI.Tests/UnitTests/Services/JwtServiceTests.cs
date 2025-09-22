using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserAuthAPI.Application.Configuration;
using UserAuthAPI.Domain.Entities;
using UserAuthAPI.Infrastructure.Services;
using Xunit;

namespace UserAuthAPI.Tests.UnitTests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    public JwtServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsATestSecretKeyForJwtTokenGeneration123456789012345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        var options = Options.Create(_jwtSettings);
        _jwtService = new JwtService(options);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtToken_WhenValidUserProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "johndoe";
        var email = "test@example.com";

        // Act
        var token = _jwtService.GenerateAccessToken(userId, username, email);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var validatedToken = tokenHandler.ReadJwtToken(token);

        validatedToken.Should().NotBeNull();
        // Check for the serialized claim type that actually appears in the token
        validatedToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == userId.ToString());
        validatedToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == email);
        validatedToken.Issuer.Should().Be(_jwtSettings.Issuer);
        validatedToken.Audiences.Should().Contain(_jwtSettings.Audience);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();
        refreshToken.Length.Should().BeGreaterThan(20);

        // Should be valid base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnDifferentTokens_OnMultipleCalls()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_ShouldReturnTrue_WhenValidTokenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "johndoe";
        var email = "test@example.com";
        var token = _jwtService.GenerateAccessToken(userId, username, email);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-token")]
    public void ValidateToken_ShouldReturnFalse_WhenInvalidTokenProvided(string invalidToken)
    {
        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetUserIdFromToken_ShouldReturnUserId_WhenValidTokenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "johndoe";
        var email = "test@example.com";
        var token = _jwtService.GenerateAccessToken(userId, username, email);

        // Act
        var extractedUserId = _jwtService.GetUserIdFromToken(token);

        // Assert
        extractedUserId.Should().Be(userId);
    }

    [Fact]
    public void GetUsernameFromToken_ShouldReturnUsername_WhenValidTokenProvided()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "johndoe";
        var email = "test@example.com";
        var token = _jwtService.GenerateAccessToken(userId, username, email);

        // Act
        var extractedUsername = _jwtService.GetUsernameFromToken(token);

        // Assert
        extractedUsername.Should().Be(username);
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeExpirationClaim()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var username = "johndoe";
        var email = "test@example.com";
        var beforeGeneration = DateTimeOffset.UtcNow;

        // Act
        var token = _jwtService.GenerateAccessToken(userId, username, email);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var validatedToken = tokenHandler.ReadJwtToken(token);

        var expClaim = validatedToken.Claims.FirstOrDefault(c => c.Type == "exp");
        expClaim.Should().NotBeNull();

        var expValue = long.Parse(expClaim!.Value);
        var expDate = DateTimeOffset.FromUnixTimeSeconds(expValue);

        expDate.Should().BeAfter(beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes - 1));
        expDate.Should().BeBefore(beforeGeneration.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes + 1));
    }
}