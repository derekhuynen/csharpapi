using Microsoft.EntityFrameworkCore;
using UserAuthAPI.Application.Interfaces;
using UserAuthAPI.Domain.Entities;
using UserAuthAPI.Infrastructure.Data;

namespace UserAuthAPI.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity operations
/// Provides Entity Framework Core-based data access for users
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the UserRepository
    /// </summary>
    /// <param name="context">The database context</param>
    public UserRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Gets all users
    /// </summary>
    /// <returns>List of all users</returns>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>The user if found, null otherwise</returns>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Gets a user by their username
    /// </summary>
    /// <param name="username">The username</param>
    /// <returns>The user if found, null otherwise</returns>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
    }

    /// <summary>
    /// Gets a user by their email address
    /// </summary>
    /// <param name="email">The email address</param>
    /// <returns>The user if found, null otherwise</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Gets a user by their username or email address
    /// </summary>
    /// <param name="usernameOrEmail">The username or email</param>
    /// <returns>The user if found, null otherwise</returns>
    public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
        {
            return null;
        }

        var lowerInput = usernameOrEmail.ToLower();
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u =>
                (u.Username != null && u.Username.ToLower() == lowerInput) ||
                u.Email.ToLower() == lowerInput);
    }

    /// <summary>
    /// Checks if a username already exists
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <returns>True if username exists, false otherwise</returns>
    public async Task<bool> UsernameExistsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        return await _context.Users
            .AnyAsync(u => u.Username != null && u.Username.ToLower() == username.ToLower());
    }

    /// <summary>
    /// Checks if an email address already exists
    /// </summary>
    /// <param name="email">The email to check</param>
    /// <returns>True if email exists, false otherwise</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    /// <param name="user">The user to create</param>
    /// <returns>The created user with generated ID</returns>
    public async Task<User> CreateAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Updates an existing user
    /// </summary>
    /// <param name="user">The user to update</param>
    /// <returns>The updated user</returns>
    public async Task<User> UpdateAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Deletes a user by their ID
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>True if deletion was successful, false otherwise</returns>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        var result = await _context.SaveChangesAsync();
        return result > 0;
    }
}