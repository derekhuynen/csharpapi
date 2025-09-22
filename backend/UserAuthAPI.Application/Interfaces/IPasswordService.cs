namespace UserAuthAPI.Application.Interfaces;

/// <summary>
/// Interface for password operations
/// Provides methods for hashing and verifying passwords securely
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a plain text password using BCrypt
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>A BCrypt hashed password string</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash
    /// </summary>
    /// <param name="password">The plain text password to verify</param>
    /// <param name="hashedPassword">The BCrypt hash to verify against</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    bool VerifyPassword(string password, string hashedPassword);
}