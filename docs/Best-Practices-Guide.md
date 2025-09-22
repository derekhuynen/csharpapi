# C# Best Practices Demonstrated

This document explains the 16 C# and .NET best practices implemented in the UserAuth API, providing educational insights for junior developers.

## 1. Clean Architecture with Dependency Injection

### Implementation

```csharp
// Program.cs - Service Registration
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
```

### Benefits

- **Testability**: Services can be easily mocked for unit testing
- **Maintainability**: Clear separation of concerns
- **Flexibility**: Easy to swap implementations
- **SOLID Principles**: Dependency Inversion Principle

### Key Pattern

```csharp
// Interface in Application layer
public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}

// Implementation in Infrastructure layer
public class PasswordService : IPasswordService
{
    // BCrypt implementation details
}
```

## 2. Async/Await Patterns Throughout

### Implementation

```csharp
public async Task<AuthResponse> LoginAsync(LoginRequest request)
{
    var user = await _userRepository.GetByEmailAsync(request.Email);
    // ... rest of implementation
}
```

### Benefits

- **Scalability**: Non-blocking I/O operations
- **Performance**: Better thread utilization
- **Responsiveness**: UI doesn't freeze during long operations

### Best Practices Demonstrated

- Always use `async Task` not `async void`
- Use `ConfigureAwait(false)` for library code
- Proper exception handling in async methods
- Cancellation token support where appropriate

## 3. Entity Framework Core with Proper Relationships

### Implementation

```csharp
// User entity with navigation properties
public class User : BaseEntity
{
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

// Fluent API configuration
public void Configure(EntityTypeBuilder<User> builder)
{
    builder.HasIndex(u => u.Email).IsUnique();
    builder.HasIndex(u => u.Username).IsUnique();

    builder.HasMany(u => u.RefreshTokens)
           .WithOne(rt => rt.User)
           .HasForeignKey(rt => rt.UserId);
}
```

### Benefits

- **Type Safety**: Compile-time checking of database operations
- **Performance**: Optimized SQL generation
- **Maintainability**: Code-first migrations
- **Relationships**: Proper foreign key constraints

## 4. JWT Token Security Implementation

### Implementation

```csharp
public string GenerateAccessToken(User user)
{
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

### Security Features

- **HMAC SHA-256**: Strong cryptographic signing
- **Short Expiration**: Limited token lifetime
- **Refresh Tokens**: Secure token renewal
- **Proper Claims**: Standard JWT claims structure

## 5. Password Hashing and Validation

### Implementation

```csharp
public string HashPassword(string password)
{
    return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
}

public bool VerifyPassword(string password, string hash)
{
    return BCrypt.Net.BCrypt.Verify(password, hash);
}
```

### Security Benefits

- **BCrypt**: Industry-standard adaptive hashing
- **Salt Rounds**: Configurable work factor (12 rounds = ~300ms)
- **Rainbow Table Resistance**: Unique salt per password
- **Future-Proof**: Can increase rounds as hardware improves

## 6. Structured Logging and Monitoring

### Implementation

```csharp
// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithProperty("Application", "UserAuthAPI")
    .CreateLogger();

// Usage in services
_logger.LogInformation("User {UserId} logged in successfully", user.Id);
_logger.LogWarning("Failed login attempt for email {Email}", request.Email);
```

### Benefits

- **Structured Data**: JSON format for log aggregation
- **Performance**: Fast, efficient logging
- **Flexibility**: Multiple sinks (console, file, cloud)
- **Security**: Sensitive data filtering

## 7. Comprehensive Error Handling

### Implementation

```csharp
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ApiErrorResponse
        {
            Message = "An error occurred while processing your request.",
            Details = exception.Message
        };

        context.Response.StatusCode = GetStatusCode(exception);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### Error Handling Strategy

- **Global Middleware**: Catches all unhandled exceptions
- **Specific Exceptions**: Custom exception types for business logic
- **Consistent Format**: Standardized error responses
- **Security**: No sensitive information leaked

## 8. Input Validation with FluentValidation

### Implementation

```csharp
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Password must contain uppercase, lowercase, number, and special character.");
    }
}
```

### Validation Benefits

- **Declarative**: Clear, readable validation rules
- **Reusable**: Validators can be shared across layers
- **Localization**: Support for multiple languages
- **Custom Rules**: Easy to create domain-specific validations

## 9. Unit and Integration Testing

### Unit Test Example

```csharp
[Fact]
public async Task LoginAsync_WithValidCredentials_ReturnsAuthResponse()
{
    // Arrange
    var user = new User { Email = "test@test.com", PasswordHash = "hashedpassword" };
    _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                      .ReturnsAsync(user);
    _mockPasswordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(true);

    // Act
    var result = await _authService.LoginAsync(new LoginRequest
    {
        Email = "test@test.com",
        Password = "password123"
    });

    // Assert
    result.Should().NotBeNull();
    result.User.Email.Should().Be("test@test.com");
}
```

### Integration Test Example

```csharp
[Fact]
public async Task Register_WithValidData_ReturnsCreatedResponse()
{
    // Arrange
    var request = new RegisterRequest
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "TestPass123!",
        FirstName = "Test",
        LastName = "User"
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/register", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### Testing Strategy

- **AAA Pattern**: Arrange, Act, Assert
- **Isolation**: Each test is independent
- **Mock Dependencies**: Focus on unit under test
- **Integration Tests**: End-to-end API testing

## 10. Environment-Specific Configuration

### Implementation

```csharp
// appsettings.Development.json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 60
  },
  "Features": {
    "EnableSwagger": true,
    "EnableDatabaseSeeding": true
  }
}

// appsettings.Production.json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15
  },
  "Features": {
    "EnableSwagger": false,
    "EnableDatabaseSeeding": false
  }
}
```

### Configuration Benefits

- **Environment Isolation**: Different settings per environment
- **Security**: Production secrets separate from development
- **Feature Toggles**: Enable/disable features per environment
- **Flexibility**: Easy configuration changes without code changes

## 11. CORS and Security Headers

### Implementation

```csharp
// CORS configuration
public static class SecurityConfiguration
{
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>();

        services.AddCors(options =>
        {
            options.AddPolicy("Development", policy =>
                policy.WithOrigins(corsOptions?.AllowedOrigins ?? Array.Empty<string>())
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials());
        });
    }
}

// Security headers middleware
public static class SecurityExtensions
{
    public static void UseBasicSecurity(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

            await next();
        });
    }
}
```

### Security Benefits

- **CORS Protection**: Controlled cross-origin access
- **Header Security**: Protection against common attacks
- **Environment Aware**: Different policies per environment

## 12. API Documentation with Swagger

### Implementation

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UserAuth API",
        Version = "v1",
        Description = "A comprehensive JWT-based authentication API"
    });

    // JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});
```

### Documentation Benefits

- **Interactive Testing**: Try endpoints directly in browser
- **API Discovery**: Clear endpoint documentation
- **Authentication Support**: JWT token testing
- **Code Generation**: Client SDKs can be generated

## 13. Repository Pattern Implementation

### Implementation

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // ... other implementations
}
```

### Repository Benefits

- **Abstraction**: Database access abstracted from business logic
- **Testability**: Easy to mock for unit tests
- **Consistency**: Standardized data access patterns
- **Flexibility**: Can swap data sources without changing business logic

## 14. Service Layer Abstraction

### Implementation

```csharp
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(LogoutRequest request);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly IJwtService _jwtService;

    // Business logic implementation
}
```

### Service Layer Benefits

- **Business Logic Centralization**: All business rules in one place
- **Reusability**: Services can be used by multiple controllers
- **Transaction Management**: Coordinate multiple repository operations
- **Validation**: Business rule validation before data persistence

## 15. Database Migrations and Seeding

### Migration Implementation

```csharp
// Migration
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                // ... other columns
            });
    }
}
```

### Seeding Implementation

```csharp
public class DatabaseSeeder
{
    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync())
        {
            _logger.LogInformation("Database already contains data. Skipping seeding.");
            return;
        }

        await SeedUsersAsync();
        await _context.SaveChangesAsync();
    }
}
```

### Migration Benefits

- **Version Control**: Database schema under source control
- **Consistency**: Same database structure across environments
- **Rollback**: Ability to undo schema changes
- **Team Collaboration**: Shared database evolution

## 16. Production-Ready Deployment Patterns

### Configuration Management

```csharp
// Environment-specific configuration
public class EnvironmentOptions
{
    public DatabaseOptions Database { get; set; }
    public FeatureOptions Features { get; set; }
    public SecurityOptions Security { get; set; }
}
```

### Health Checks

```csharp
app.MapGet("/api/health", () => new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0",
    environment = app.Environment.EnvironmentName
});
```

### Deployment Benefits

- **Health Monitoring**: Application health visibility
- **Configuration Management**: Environment-specific settings
- **Logging**: Production-ready logging configuration
- **Security**: HTTPS enforcement and security headers

## Summary

These 16 best practices create a production-ready API that demonstrates:

1. **Maintainability** through clean architecture
2. **Scalability** through async patterns
3. **Security** through proper authentication and validation
4. **Reliability** through comprehensive testing
5. **Observability** through structured logging
6. **Flexibility** through configuration management

Each pattern serves both educational and practical purposes, showing junior developers how to build robust, secure, and maintainable web APIs using modern C# and .NET practices.
