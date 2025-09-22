using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using UserAuthAPI.Application.Configuration;
using UserAuthAPI.Application.Interfaces;
using UserAuthAPI.Infrastructure.Data;
using UserAuthAPI.Infrastructure.Repositories;
using UserAuthAPI.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using UserAuthAPI.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// Add Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=userauth.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Configure JWT Settings
builder.Services.Configure<JwtSettings>(options =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt");
    if (jwtSection.Exists())
    {
        jwtSection.Bind(options);
    }
    else
    {
        // Default settings for testing
        options.SecretKey = "default-secret-key-for-testing-that-is-at-least-32-characters-long";
        options.Issuer = "default-issuer";
        options.Audience = "default-audience";
        options.AccessTokenExpirationMinutes = 60;
        options.RefreshTokenExpirationDays = 7;
    }
});

// Register application services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Configure JWT Authentication with safe defaults
var defaultSecretKey = "default-secret-key-for-testing-that-is-at-least-32-characters-long";
var defaultIssuer = "default-issuer";
var defaultAudience = "default-audience";

var secretKey = builder.Configuration["Jwt:SecretKey"] ?? defaultSecretKey;
var issuer = builder.Configuration["Jwt:Issuer"] ?? defaultIssuer;
var audience = builder.Configuration["Jwt:Audience"] ?? defaultAudience;

// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "YourIssuer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "YourAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:SecretKey"] ?? "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!"))
        };
    });

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
