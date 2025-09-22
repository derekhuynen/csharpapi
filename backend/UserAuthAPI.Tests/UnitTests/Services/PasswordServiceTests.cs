using FluentAssertions;
using UserAuthAPI.Infrastructure.Services;
using Xunit;

namespace UserAuthAPI.Tests.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword_WhenValidPasswordProvided()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hashedPassword = _passwordService.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Length.Should().Be(60); // BCrypt hash length
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashes_ForSamePassword()
    {
        // Arrange
        var password = "TestPassword123!";

        // Act
        var hash1 = _passwordService.HashPassword(password);
        var hash2 = _passwordService.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "TestPassword123!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(password, hashedPassword);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var password = "TestPassword123!";
        var wrongPassword = "WrongPassword456!";
        var hashedPassword = _passwordService.HashPassword(password);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, hashedPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsNullOrEmpty(string invalidPassword)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.HashPassword(invalidPassword));
    }

    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenPasswordIsNull()
    {
        // Arrange
        var hashedPassword = _passwordService.HashPassword("ValidPassword123!");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.VerifyPassword(null!, hashedPassword));
    }

    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenHashIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _passwordService.VerifyPassword("ValidPassword123!", null!));
    }
}