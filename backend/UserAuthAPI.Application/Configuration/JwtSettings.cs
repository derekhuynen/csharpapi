namespace UserAuthAPI.Application.Configuration;

/// <summary>
/// Configuration settings for JWT token generation and validation
/// Maps to the "Jwt" section in appsettings.json
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// The configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// The secret key used for signing JWT tokens
    /// Should be at least 256 bits (32 characters) for HS256
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// The issuer of the JWT tokens (typically the application name)
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The intended audience for the JWT tokens
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The expiration time for access tokens in minutes
    /// Default is 15 minutes for security
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// The expiration time for refresh tokens in days
    /// Default is 7 days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}