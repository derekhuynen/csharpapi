using UserAuthAPI.Domain.Entities;

namespace UserAuthAPI.Application.Interfaces;

/// <summary>
/// Repository interface for User entity operations
/// Provides data access methods for user management
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of all users</returns>
    Task<IEnumerable<User>> GetAllAsync();

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a user by their username
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByUsernameAsync(string username);

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    /// <param name="email">The email address</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Gets a user by their username or email address
    /// </summary>
    /// <param name="usernameOrEmail">The username or email</param>
    /// <returns>The user if found, null otherwise</returns>
    Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);

    /// <summary>
    /// Checks if a username already exists
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <returns>True if username exists, false otherwise</returns>
    Task<bool> UsernameExistsAsync(string username);

    /// <summary>
    /// Checks if an email address already exists
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <returns>True if email exists, false otherwise</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>The created user with generated ID</returns>
    Task<User> CreateAsync(User user);

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>The updated user</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Deletes a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id);
}