namespace UserAuthAPI.Application.Interfaces;

/// <summary>
/// Interface for JWT token operations
/// Provides methods for generating, validating, and refreshing JWT tokens
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// Generates a JWT access token for the specified user
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="username">The username</param>
    /// <param name="email">The user's email address</param>
    /// <returns>A JWT access token string</returns>
    string GenerateAccessToken(Guid userId, string? username, string email);

    /// <summary>
    /// Generates a secure refresh token
    /// </summary>
    /// <returns>A cryptographically secure refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT access token
    /// </summary>
    /// <param name="token">The token to validate</param>
    /// <returns>True if the token is valid, false otherwise</returns>
    bool ValidateToken(string token);

    /// <summary>
    /// Extracts the user ID from a valid JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The user ID if extraction is successful, null otherwise</returns>
    Guid? GetUserIdFromToken(string token);

    /// <summary>
    /// Extracts the username from a valid JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The username if extraction is successful, null otherwise</returns>
    string? GetUsernameFromToken(string token);
}