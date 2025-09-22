# UserAuthAPI - System Design Document

## 📋 Project Overview

**Project Name:** UserAuthAPI  
**Purpose:** Educational C# REST API demonstrating authentication best practices  
**Target Framework:** .NET 8  
**Database:** SQLite (for easy local development)  
**Architecture:** Clean Architecture with Domain-Driven Design principles

## 🎯 Learning Objectives

This project demonstrates the following C# best practices:

1. ✅ Dependency Injection
2. ✅ Async/Await patterns
3. ✅ IActionResult for API responses
4. ✅ Input validation with FluentValidation
5. ✅ Structured logging with Serilog
6. ✅ Clean folder organization
7. ✅ Entity Framework Core for data access
8. ✅ AutoMapper for object mapping
9. ✅ Global exception handling
10. ✅ Configuration management
11. ✅ Swagger/OpenAPI documentation
12. ✅ Pagination for list endpoints
13. ✅ Environment variables for secrets
14. ✅ RESTful API design
15. ✅ Custom middleware
16. ✅ API versioning

## 🏗️ Architecture Overview

```
UserAuthAPI/
├── UserAuthAPI.Api/              # Presentation Layer
│   ├── Controllers/              # API Controllers
│   ├── Middleware/               # Custom middleware
│   ├── Filters/                  # Action filters
│   └── Program.cs               # Application entry point
├── UserAuthAPI.Application/      # Application Layer
│   ├── Services/                # Business logic services
│   ├── DTOs/                    # Data Transfer Objects
│   ├── Validators/              # Input validation
│   ├── Mappings/                # AutoMapper profiles
│   └── Interfaces/              # Service contracts
├── UserAuthAPI.Domain/           # Domain Layer
│   ├── Entities/                # Domain models
│   ├── Enums/                   # Domain enumerations
│   └── ValueObjects/            # Value objects
├── UserAuthAPI.Infrastructure/   # Infrastructure Layer
│   ├── Data/                    # EF Core DbContext
│   ├── Repositories/            # Data access layer
│   ├── Services/                # External services
│   └── Configuration/           # Infrastructure setup
└── UserAuthAPI.Tests/           # Test Layer
    ├── Unit/                    # Unit tests
    ├── Integration/             # Integration tests
    └── Helpers/                 # Test utilities
```

## 📊 Database Schema

### Users Table

```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### RefreshTokens Table

```sql
CREATE TABLE RefreshTokens (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Token NVARCHAR(255) NOT NULL UNIQUE,
    UserId INTEGER NOT NULL,
    ExpiryDate DATETIME NOT NULL,
    IsUsed BOOLEAN NOT NULL DEFAULT 0,
    IsRevoked BOOLEAN NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

## 🔌 API Endpoints

### Authentication Endpoints

| Method | Endpoint                             | Description               | Auth Required |
| ------ | ------------------------------------ | ------------------------- | ------------- |
| POST   | `/api/v1/auth/register`              | Register new user         | No            |
| POST   | `/api/v1/auth/login`                 | User login                | No            |
| POST   | `/api/v1/auth/refresh`               | Refresh access token      | No            |
| POST   | `/api/v1/auth/logout`                | User logout               | Yes           |
| POST   | `/api/v1/auth/revoke-refresh-tokens` | Revoke all refresh tokens | Yes           |

### User Management Endpoints

| Method | Endpoint             | Description              | Auth Required |
| ------ | -------------------- | ------------------------ | ------------- |
| GET    | `/api/v1/users/{id}` | Get user by ID           | Yes           |
| GET    | `/api/v1/users`      | Get paginated users list | Yes           |
| PUT    | `/api/v1/users/{id}` | Update user profile      | Yes           |
| DELETE | `/api/v1/users/{id}` | Delete user account      | Yes           |

## 📝 Data Transfer Objects (DTOs)

### Request DTOs

```csharp
public class RegisterRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; }
}
```

### Response DTOs

```csharp
public class AuthResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## 🔐 Authentication Flow

### Registration Process

1. User submits registration data
2. Validate input using FluentValidation
3. Check if username/email already exists
4. Hash password using BCrypt
5. Save user to database
6. Generate JWT access token and refresh token
7. Return authentication response

### Login Process

1. User submits credentials
2. Validate input
3. Verify user exists and password is correct
4. Generate new JWT access token and refresh token
5. Save refresh token to database
6. Return authentication response

### Token Refresh Process

1. User submits refresh token
2. Validate refresh token exists and is not expired/revoked
3. Generate new access token
4. Optionally rotate refresh token
5. Return new tokens

## 🛠️ Technology Stack

### Core Framework

- **.NET 8** - Latest LTS version
- **ASP.NET Core** - Web API framework

### Database & ORM

- **SQLite** - Lightweight database for local development
- **Entity Framework Core** - Object-relational mapping

### Authentication & Security

- **JWT Bearer Tokens** - Stateless authentication
- **BCrypt** - Password hashing
- **Data Protection API** - Token encryption

### Validation & Mapping

- **FluentValidation** - Input validation
- **AutoMapper** - Object-to-object mapping

### Logging & Documentation

- **Serilog** - Structured logging
- **Swagger/OpenAPI** - API documentation
- **Swashbuckle** - Swagger UI integration

### Testing

- **xUnit** - Testing framework
- **Moq** - Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing

## ⚙️ Configuration

### JWT Settings

```json
{
	"JwtSettings": {
		"SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
		"Issuer": "UserAuthAPI",
		"Audience": "UserAuthAPI-Client",
		"AccessTokenExpirationMinutes": 15,
		"RefreshTokenExpirationDays": 7
	}
}
```

### Database Settings

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=userauth.db"
	}
}
```

### Logging Configuration

```json
{
	"Serilog": {
		"MinimumLevel": "Information",
		"WriteTo": [
			{
				"Name": "Console"
			},
			{
				"Name": "File",
				"Args": {
					"path": "logs/app-.txt",
					"rollingInterval": "Day"
				}
			}
		]
	}
}
```

## 🧪 Testing Strategy

### Unit Tests

- **Controller Tests** - Test HTTP endpoints
- **Service Tests** - Test business logic
- **Validator Tests** - Test input validation
- **Repository Tests** - Test data access

### Integration Tests

- **API Integration Tests** - End-to-end endpoint testing
- **Database Integration Tests** - Test EF Core operations

## 🚀 Deployment Considerations

### Environment Variables

```bash
JWT_SECRET_KEY=your-production-secret-key
DATABASE_CONNECTION=your-production-db-connection
ASPNETCORE_ENVIRONMENT=Production
```

### Docker Support

- Dockerfile for containerization
- Docker Compose for local development

## 📚 Learning Resources

### Key Concepts Demonstrated

1. **Clean Architecture** - Separation of concerns
2. **SOLID Principles** - Object-oriented design
3. **Repository Pattern** - Data access abstraction
4. **Unit of Work** - Transaction management
5. **Middleware Pipeline** - Request/response processing
6. **Dependency Injection** - Inversion of control
7. **JWT Authentication** - Stateless security
8. **RESTful APIs** - HTTP best practices

## 🎓 Next Steps for Junior Developers

After completing this project, consider learning:

1. **Message Queues** - RabbitMQ, Azure Service Bus
2. **Caching** - Redis, In-Memory caching
3. **Microservices** - Service decomposition
4. **Event Sourcing** - Event-driven architecture
5. **CQRS** - Command Query Responsibility Segregation
6. **API Rate Limiting** - Throttling requests
7. **Health Checks** - Application monitoring
8. **Background Services** - Hosted services

---

_This document serves as a comprehensive guide for implementing a production-ready authentication API while demonstrating C# best practices._
