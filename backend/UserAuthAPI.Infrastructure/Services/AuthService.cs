using Microsoft.Extensions.Options;
using UserAuthAPI.Application.Configuration;
using UserAuthAPI.Application.Interfaces;
using UserAuthAPI.Domain.Entities;

namespace UserAuthAPI.Infrastructure.Services;

/// <summary>
/// Service for handling authentication operations
/// Coordinates between password hashing, JWT generation, and user management
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// Initializes a new instance of the AuthService
    /// </summary>
    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordService passwordService,
        IJwtService jwtService,
        IOptions<JwtSettings> jwtSettings)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    /// <summary>
    /// Registers a new user with the provided information
    /// Validates uniqueness of username and email before creating the user
    /// </summary>
    public async Task<AuthResult> RegisterAsync(User user, string password, string? ipAddress = null, string? userAgent = null)
    {
        // Validate input
        if (user == null)
            return AuthResult.CreateFailure("User information is required");

        if (string.IsNullOrWhiteSpace(user.Email))
            return AuthResult.CreateFailure("Email is required");

        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.CreateFailure("Password is required");

        if (string.IsNullOrWhiteSpace(user.FirstName))
            return AuthResult.CreateFailure("First name is required");

        if (string.IsNullOrWhiteSpace(user.LastName))
            return AuthResult.CreateFailure("Last name is required");

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(user.Email))
            return AuthResult.CreateFailure("A user with this email already exists");

        // Check if username already exists (if provided)
        if (!string.IsNullOrEmpty(user.Username) && await _userRepository.UsernameExistsAsync(user.Username))
            return AuthResult.CreateFailure("A user with this username already exists");

        try
        {
            // Hash the password
            user.PasswordHash = _passwordService.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            var createdUser = await _userRepository.CreateAsync(user);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(createdUser.Id, createdUser.Username, createdUser.Email);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = createdUser.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

            return AuthResult.CreateSuccess(accessToken, refreshToken, createdUser, expiresAt);
        }
        catch (Exception ex)
        {
            return AuthResult.CreateFailure($"An error occurred during registration: {ex.Message}");
        }
    }

    /// <summary>
    /// Authenticates a user with username/email and password
    /// Updates the user's last login time upon successful authentication
    /// </summary>
    public async Task<AuthResult> LoginAsync(string usernameOrEmail, string password, string? ipAddress = null, string? userAgent = null)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            return AuthResult.CreateFailure("Username or email is required");

        if (string.IsNullOrWhiteSpace(password))
            return AuthResult.CreateFailure("Password is required");

        try
        {
            // Find user by username or email
            var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
            if (user == null)
                return AuthResult.CreateFailure("Invalid username/email or password");

            // Check if user is active
            if (!user.IsActive)
                return AuthResult.CreateFailure("User account is deactivated");

            // Verify password
            if (!_passwordService.VerifyPassword(password, user.PasswordHash))
                return AuthResult.CreateFailure("Invalid username/email or password");

            // Update last login time
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Username, user.Email);
            var refreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

            return AuthResult.CreateSuccess(accessToken, refreshToken, user, expiresAt);
        }
        catch (Exception ex)
        {
            return AuthResult.CreateFailure($"An error occurred during login: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// Generates new access and refresh tokens while revoking the old refresh token
    /// </summary>
    public async Task<AuthResult> RefreshTokenAsync(string refreshToken, string? ipAddress = null, string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return AuthResult.CreateFailure("Refresh token is required");

        try
        {
            // Find the refresh token
            var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (storedRefreshToken == null)
                return AuthResult.CreateFailure("Invalid refresh token");

            // Check if token is still valid
            if (!storedRefreshToken.IsValid)
                return AuthResult.CreateFailure("Refresh token is expired or revoked");

            // Get the user
            var user = await _userRepository.GetByIdAsync(storedRefreshToken.UserId);
            if (user == null || !user.IsActive)
                return AuthResult.CreateFailure("User not found or deactivated");

            // Revoke the old refresh token
            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Username, user.Email);
            var newRefreshToken = _jwtService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _refreshTokenRepository.CreateAsync(newRefreshTokenEntity);

            return AuthResult.CreateSuccess(newAccessToken, newRefreshToken, user, expiresAt);
        }
        catch (Exception ex)
        {
            return AuthResult.CreateFailure($"An error occurred during token refresh: {ex.Message}");
        }
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token
    /// </summary>
    public async Task<bool> LogoutAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        try
        {
            var storedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (storedRefreshToken == null || storedRefreshToken.IsRevoked)
                return false;

            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(storedRefreshToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Revokes all refresh tokens for a specific user (logout from all devices)
    /// </summary>
    public async Task<bool> RevokeAllTokensAsync(Guid userId)
    {
        try
        {
            var revokedCount = await _refreshTokenRepository.RevokeAllTokensForUserAsync(userId);
            return revokedCount > 0;
        }
        catch
        {
            return false;
        }
    }
}