using BCrypt.Net;
using UserAuthAPI.Application.Interfaces;

namespace UserAuthAPI.Infrastructure.Services;

/// <summary>
/// Service for password hashing and verification using BCrypt
/// Implements secure password storage following best practices
/// </summary>
public class PasswordService : IPasswordService
{
    /// <summary>
    /// Hashes a plain text password using BCrypt with work factor 12
    /// Work factor 12 provides good security while maintaining reasonable performance
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>A BCrypt hashed password string</returns>
    /// <exception cref="ArgumentException">Thrown when password is null or empty</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        // BCrypt with work factor 12 (2^12 = 4096 rounds)
        // This provides good security while maintaining reasonable performance
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash
    /// Uses constant-time comparison to prevent timing attacks
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The BCrypt hash to verify against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when password or hashedPassword is null or empty</exception>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));
        }

        try
        {
            // BCrypt.Verify handles constant-time comparison to prevent timing attacks
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception)
        {
            // If hash is malformed or verification fails, return false
            return false;
        }
    }
}