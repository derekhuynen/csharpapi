namespace UserAuthAPI.Api.Configuration;

public static class SecurityConfiguration
{
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        services.AddCors(options =>
        {
            options.AddPolicy("Development", policy =>
            {
                var allowedOrigins = corsOptions.AllowedOrigins.Any()
                    ? corsOptions.AllowedOrigins
                    : new[] { "http://localhost:3000", "https://localhost:3001", "http://localhost:5173" };

                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("Production", policy =>
            {
                var allowedOrigins = corsOptions.AllowedOrigins.Any()
                    ? corsOptions.AllowedOrigins
                    : new[] { "https://yourdomain.com" };

                policy
                    .WithOrigins(allowedOrigins)
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                    .WithHeaders("Content-Type", "Authorization")
                    .AllowCredentials();
            });
        });
    }

    public static void UseBasicSecurity(this WebApplication app)
    {
        var securityOptions = app.Configuration.GetSection(SecurityOptions.SectionName).Get<SecurityOptions>() ?? new SecurityOptions();

        // Basic security headers
        app.Use(async (context, next) =>
        {
            // Prevent clickjacking
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // Prevent MIME sniffing
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // XSS Protection
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Add HSTS header in production
            if (securityOptions.StrictTransportSecurity)
            {
                context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            // Remove server info
            context.Response.Headers.Remove("Server");
            context.Response.Headers.Remove("X-Powered-By");

            await next();
        });
    }
}