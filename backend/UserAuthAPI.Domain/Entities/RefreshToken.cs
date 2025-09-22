using System.ComponentModel.DataAnnotations;

namespace UserAuthAPI.Domain.Entities;

/// <summary>
/// RefreshToken entity for JWT token refresh functionality
/// Stores refresh tokens and their metadata for secure token renewal
/// </summary>
public class RefreshToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the refresh token value
    /// This is a cryptographically secure random string
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user this token belongs to
    /// Foreign key to the User entity
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the expiration date and time for the token
    /// After this time, the token is no longer valid
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets whether the token has been revoked
    /// Revoked tokens cannot be used even if not expired
    /// </summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>
    /// Gets or sets the date and time when the token was revoked
    /// Null if the token has not been revoked
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the token was created
    /// Used for security auditing and suspicious activity detection
    /// </summary>
    [StringLength(45)] // IPv6 max length
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the client that created the token
    /// Used for security auditing and device identification
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets whether the token is currently valid
    /// A token is valid if it's not expired and not revoked
    /// </summary>
    public bool IsValid => !IsExpired && !IsRevoked;

    /// <summary>
    /// Gets whether the token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Navigation property to the User that owns this refresh token
    /// </summary>
    public virtual User User { get; set; } = null!;
}