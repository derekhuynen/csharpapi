using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace UserAuthAPI.Infrastructure.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext
/// Required for Entity Framework Core migrations and design-time operations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Creates a new instance of ApplicationDbContext for design-time operations
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>A configured ApplicationDbContext instance</returns>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        // Get connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=userauth.db";

        // Configure DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}