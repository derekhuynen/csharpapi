using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserAuthAPI.Domain.Entities;
using UserAuthAPI.Application.Interfaces;

namespace UserAuthAPI.Infrastructure.Data;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context,
        IPasswordService passwordService,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await _context.Users.AnyAsync())
            {
                var userCount = await _context.Users.CountAsync();
                _logger.LogInformation("Database already contains {UserCount} users. Skipping seeding.", userCount);
                return;
            }

            _logger.LogInformation("Starting database seeding...");

            await SeedUsersAsync();

            await _context.SaveChangesAsync();

            var finalUserCount = await _context.Users.CountAsync();
            _logger.LogInformation("Database seeding completed successfully. Total users: {UserCount}", finalUserCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedUsersAsync()
    {
        _logger.LogInformation("Seeding users...");

        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                PasswordHash = _passwordService.HashPassword("Admin123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "john.doe",
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe",
                PasswordHash = _passwordService.HashPassword("JohnDoe123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "jane.smith",
                Email = "jane.smith@example.com",
                FirstName = "Jane",
                LastName = "Smith",
                PasswordHash = _passwordService.HashPassword("JaneSmith123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "demo",
                Email = "demo@example.com",
                FirstName = "Demo",
                LastName = "User",
                PasswordHash = _passwordService.HashPassword("Demo123!"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await _context.Users.AddRangeAsync(users);

        _logger.LogInformation("Added {UserCount} users to the database.", users.Count);
    }
}