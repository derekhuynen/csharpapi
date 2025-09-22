using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UserAuthAPI.Infrastructure.Data;

namespace UserAuthAPI.Infrastructure.Extensions;

public static class HostExtensions
{
    /// <summary>
    /// Seeds the database with initial data during application startup.
    /// </summary>
    /// <param name="host">The application host</param>
    /// <param name="enableSeeding">Enable seeding based on configuration or environment</param>
    /// <returns>The host for method chaining</returns>
    public static async Task<IHost> SeedDatabaseAsync(this IHost host, bool enableSeeding = true)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
        var environment = services.GetRequiredService<IHostEnvironment>();

        try
        {
            // Only seed when enabled
            if (!enableSeeding)
            {
                logger.LogInformation("Database seeding is disabled. Environment: {Environment}", environment.EnvironmentName);
                return host;
            }

            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred seeding the database.");

            // In development, we want to see seeding errors immediately
            if (environment.IsDevelopment())
            {
                throw;
            }
        }

        return host;
    }
}