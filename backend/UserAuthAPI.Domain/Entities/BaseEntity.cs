using System.ComponentModel.DataAnnotations;

namespace UserAuthAPI.Domain.Entities;

/// <summary>
/// Base entity class providing common properties for all domain entities
/// Includes audit fields and primary key definition
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was created
    /// Automatically set to UTC when entity is first saved
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated
    /// Automatically updated to UTC whenever entity is modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the entity was last accessed for login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}