using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using UserAuthAPI.Application.Configuration;
using UserAuthAPI.Application.Interfaces;

namespace UserAuthAPI.Infrastructure.Services;

/// <summary>
/// Service for handling JWT token operations
/// Provides functionality for generating, validating, and extracting information from JWT tokens
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly byte[] _secretKey;

    /// <summary>
    /// Initializes a new instance of the JwtService
    /// </summary>
    /// <param name="jwtSettings">JWT configuration settings</param>
    /// <exception cref="ArgumentNullException">Thrown when jwtSettings is null</exception>
    /// <exception cref="ArgumentException">Thrown when secret key is invalid</exception>
    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));

        if (string.IsNullOrEmpty(_jwtSettings.SecretKey))
        {
            throw new ArgumentException("JWT SecretKey cannot be null or empty", nameof(jwtSettings));
        }

        if (_jwtSettings.SecretKey.Length < 32)
        {
            throw new ArgumentException("JWT SecretKey must be at least 32 characters long for security", nameof(jwtSettings));
        }

        var keyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
        if (keyBytes.Length < 32) // 256 bits minimum
        {
            throw new ArgumentException("JWT SecretKey must be at least 256 bits (32 bytes) when UTF-8 encoded", nameof(jwtSettings));
        }

        _secretKey = keyBytes;
    }

    /// <summary>
    /// Generates a JWT access token for the specified user
    /// Token includes user ID, username, email, and standard JWT claims
    /// </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="username">The username</param>
    /// <param name="email">The user's email address</param>
    /// <returns>A JWT access token string</returns>
    public string GenerateAccessToken(Guid userId, string? username, string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(_secretKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(username))
        {
            claims.Add(new Claim(ClaimTypes.Name, username));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token
    /// Uses a random 32-byte array encoded as base64
    /// </summary>
    /// <returns>A base64-encoded refresh token string</returns>
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Validates a JWT access token
    /// Checks signature, expiration, issuer, and audience
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>True if the token is valid, false otherwise</returns>
    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(_secretKey),
                ClockSkew = TimeSpan.Zero // Remove default 5-minute tolerance for token expiration
            };

            tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts the user ID from a valid JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The user ID if extraction is successful, null otherwise</returns>
    public Guid? GetUserIdFromToken(string token)
    {
        if (!ValidateToken(token))
        {
            return null;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Try both the long and short form of the claim
            var userIdClaim = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.NameIdentifier ||
                x.Type == "nameid" ||
                x.Type == JwtRegisteredClaimNames.Sub ||
                x.Type == "sub");

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the username from a valid JWT token
    /// </summary>
    /// <param name="token">The JWT token</param>
    /// <returns>The username if extraction is successful, null otherwise</returns>
    public string? GetUsernameFromToken(string token)
    {
        if (!ValidateToken(token))
        {
            return null;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            // Try both the long and short form of the claim
            var usernameClaim = jwtToken.Claims.FirstOrDefault(x =>
                x.Type == ClaimTypes.Name ||
                x.Type == "unique_name");

            return usernameClaim?.Value;
        }
        catch
        {
            return null;
        }
    }
}