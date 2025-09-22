using System.ComponentModel.DataAnnotations;

namespace UserAuthAPI.Domain.Entities;

/// <summary>
/// User entity representing a registered user in the system
/// Contains authentication and profile information
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique username for the user
    /// Must be unique across the system and is used for login
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address
    /// Must be unique and is used for login and notifications
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password for the user
    /// Stored as a BCrypt hash for security
    /// </summary>
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's first name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name
    /// </summary>
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user account is active
    /// Inactive users cannot log in
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets the user's full name by combining first and last name
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Navigation property for the user's refresh tokens
    /// Used for JWT token refresh functionality
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}