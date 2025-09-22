namespace UserAuthAPI.Api.Configuration;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableDetailedErrors { get; set; }
}

public class CorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class FeatureOptions
{
    public const string SectionName = "Features";

    public bool EnableSwagger { get; set; } = true;
    public bool EnableDetailedErrors { get; set; } = true;
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableDatabaseSeeding { get; set; } = true;
}

public class SecurityOptions
{
    public const string SectionName = "Security";

    public bool RequireHttps { get; set; } = false;
    public bool StrictTransportSecurity { get; set; } = false;
}