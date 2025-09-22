using Microsoft.EntityFrameworkCore;
using UserAuthAPI.Application.Interfaces;
using UserAuthAPI.Domain.Entities;
using UserAuthAPI.Infrastructure.Data;

namespace UserAuthAPI.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for RefreshToken entity operations
/// Provides Entity Framework Core-based data access for refresh tokens
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the RefreshTokenRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public RefreshTokenRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets a refresh token by its token value
    /// </summary>
    /// <param name="token">The refresh token value</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    /// <summary>
    /// Gets all refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of refresh tokens for the user</returns>
    public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a new refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to create</param>
    /// <returns>The created refresh token</returns>
    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        if (refreshToken == null)
        {
            throw new ArgumentNullException(nameof(refreshToken));
        }

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    /// <summary>
    /// Updates an existing refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to update</param>
    /// <returns>The updated refresh token</returns>
    public async Task<RefreshToken> UpdateAsync(RefreshToken refreshToken)
    {
        if (refreshToken == null)
        {
            throw new ArgumentNullException(nameof(refreshToken));
        }

        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }

    /// <summary>
    /// Deletes expired refresh tokens
    /// Uses raw SQL for better performance with bulk operations
    /// </summary>
    /// <returns>Number of tokens deleted</returns>
    public async Task<int> DeleteExpiredTokensAsync()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            return await _context.SaveChangesAsync();
        }

        return 0;
    }

    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// Sets IsRevoked flag and RevokedAt timestamp
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Number of tokens revoked</returns>
    public async Task<int> RevokeAllTokensForUserAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        if (tokens.Any())
        {
            var revokedAt = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = revokedAt;
            }

            return await _context.SaveChangesAsync();
        }

        return 0;
    }
}