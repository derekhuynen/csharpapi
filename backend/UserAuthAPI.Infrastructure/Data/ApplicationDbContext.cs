using Microsoft.EntityFrameworkCore;
using UserAuthAPI.Domain.Entities;

namespace UserAuthAPI.Infrastructure.Data;

/// <summary>
/// The main Entity Framework database context for the UserAuth API
/// Handles data access and entity configuration for all domain entities
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext
    /// </summary>
    /// <param name="options">The DbContext options</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the RefreshTokens DbSet
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Configures the entity models using Fluent API
    /// </summary>
    /// <param name="modelBuilder">The model builder instance</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Username)
                .IsUnique()
                .HasDatabaseName("IX_Users_Username");

            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime('now')")
                .ValueGeneratedOnAddOrUpdate();

            // Configure relationship with RefreshTokens
            entity.HasMany(e => e.RefreshTokens)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure RefreshToken entity
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Token)
                .IsUnique()
                .HasDatabaseName("IX_RefreshTokens_Token");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_RefreshTokens_UserId");

            entity.Property(e => e.Token)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45); // IPv6 max length

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("datetime('now')")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("datetime('now')")
                .ValueGeneratedOnAddOrUpdate();

            // Configure relationship with User
            entity.HasOne(e => e.User)
                .WithMany(e => e.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Override SaveChanges to automatically update timestamps
    /// </summary>
    /// <returns>Number of entities written to the database</returns>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to automatically update timestamps
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of entities written to the database</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates the UpdatedAt property for modified entities
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            var entity = (BaseEntity)entityEntry.Entity;

            if (entityEntry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            if (entityEntry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}