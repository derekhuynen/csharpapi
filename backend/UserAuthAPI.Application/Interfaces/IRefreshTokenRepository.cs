using UserAuthAPI.Domain.Entities;

namespace UserAuthAPI.Application.Interfaces;

/// <summary>
/// Repository interface for RefreshToken entity operations
/// Provides data access methods for refresh token management
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its token value
    /// </summary>
    /// <param name="token">The refresh token value</param>
    /// <returns>The refresh token if found, null otherwise</returns>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Gets all refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of refresh tokens for the user</returns>
    Task<List<RefreshToken>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Creates a new refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to create</param>
    /// <returns>The created refresh token</returns>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);

    /// <summary>
    /// Updates an existing refresh token
    /// </summary>
    /// <param name="refreshToken">The refresh token to update</param>
    /// <returns>The updated refresh token</returns>
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);

    /// <summary>
    /// Deletes expired refresh tokens
    /// </summary>
    /// <returns>Number of tokens deleted</returns>
    Task<int> DeleteExpiredTokensAsync();

    /// <summary>
    /// Revokes all refresh tokens for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllTokensForUserAsync(Guid userId);
}