using UserAuthAPI.Domain.Entities;

namespace UserAuthAPI.Application.Interfaces;

/// <summary>
/// Interface for authentication operations
/// Provides methods for user registration, login, token refresh, and logout
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user with the provided information
    /// </summary>
    /// <param name="user">The user entity to register</param>
    /// <param name="password">The user's password (will be hashed)</param>
    /// <param name="ipAddress">The IP address of the requesting client</param>
    /// <param name="userAgent">The user agent string of the requesting client</param>
    /// <returns>A task that resolves to the authentication result</returns>
    Task<AuthResult> RegisterAsync(User user, string password, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Authenticates a user with username/email and password
    /// </summary>
    /// <param name="usernameOrEmail">The username or email address</param>
    /// <param name="password">The user's password</param>
    /// <param name="ipAddress">The IP address of the requesting client</param>
    /// <param name="userAgent">The user agent string of the requesting client</param>
    /// <returns>A task that resolves to the authentication result</returns>
    Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token</param>
    /// <param name="ipAddress">The IP address of the requesting client</param>
    /// <param name="userAgent">The user agent string of the requesting client</param>
    /// <returns>A task that resolves to the authentication result</returns>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null);

    /// <summary>
    /// Logs out a user by revoking their refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke</param>
    /// <returns>A task that resolves to true if logout was successful</returns>
    Task<bool> LogoutAsync(string refreshToken);

    /// <summary>
    /// Revokes all refresh tokens for a specific user (logout from all devices)
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>A task that resolves to true if all tokens were revoked</returns>
    Task<bool> RevokeAllTokensAsync(Guid userId);
}

/// <summary>
/// Result of an authentication operation
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Whether the authentication was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if authentication failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The JWT access token (if authentication was successful)
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// The refresh token (if authentication was successful)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// The authenticated user (if authentication was successful)
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// The user ID (if authentication was successful)
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The expiration time of the access token
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Creates a successful authentication result
    /// </summary>
    public static AuthResult CreateSuccess(string accessToken, string refreshToken, User user, DateTime expiresAt)
    {
        return new AuthResult
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = user,
            UserId = user.Id,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Creates a failed authentication result
    /// </summary>
    public static AuthResult CreateFailure(string errorMessage)
    {
        return new AuthResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}