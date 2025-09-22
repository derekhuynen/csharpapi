# UserAuth API - JWT Authentication System

A comprehensive C# REST API demonstrating modern authentication patterns and best practices built with Clean Architecture principles. This project serves as an educational resource for junior developers learning C# web development, Entity Framework Core, JWT authentication, and production-ready API design.

## 🏗️ Architecture Overview

This API follows **Clean Architecture** principles with clear separation of concerns:

```
📁 UserAuthAPI.Domain         # Core business entities and rules
📁 UserAuthAPI.Application    # Business logic and interfaces
📁 UserAuthAPI.Infrastructure # Data access and external services
📁 UserAuthAPI.Api           # Web API controllers and configuration
📁 UserAuthAPI.Tests         # Comprehensive test suite
```

## ✨ Key Features

### 🔐 Authentication & Security

- **JWT Bearer Token Authentication** with refresh tokens
- **BCrypt Password Hashing** for secure password storage
- **Role-based Authorization** ready for extension
- **CORS Configuration** for cross-origin requests
- **Security Headers** middleware for production safety

### 🛠️ Development Experience

- **Swagger/OpenAPI Documentation** with interactive testing
- **Comprehensive Logging** with Serilog structured logging
- **Environment-Specific Configuration** (Development/Production)
- **Database Seeding** with demo users for instant testing
- **Global Exception Handling** with detailed error responses

### 🧪 Quality Assurance

- **FluentValidation** for robust input validation
- **Unit & Integration Tests** with xUnit and FluentAssertions
- **Test Coverage** for critical authentication flows
- **Development Tools** for debugging and verification

### 📚 Educational Value

Demonstrates **16 C# Best Practices**:

1. Clean Architecture with dependency injection
2. Async/await patterns throughout
3. Entity Framework Core with proper relationships
4. JWT token security implementation
5. Password hashing and validation
6. Structured logging and monitoring
7. Comprehensive error handling
8. Input validation with FluentValidation
9. Unit and integration testing
10. Environment-specific configuration
11. CORS and security headers
12. API documentation with Swagger
13. Repository pattern implementation
14. Service layer abstraction
15. Database migrations and seeding
16. Production-ready deployment patterns

## 🚀 Quick Start

### Prerequisites

- **.NET 8.0 SDK** or later
- **Git** for version control
- **VS Code** or **Visual Studio** (recommended)
- **SQLite** (included with .NET)

### 1. Clone and Setup

```bash
git clone <repository-url>
cd csharpapi/backend
dotnet restore
```

### 2. Run Database Migrations

```bash
dotnet ef database update --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api
```

### 3. Start the API

```bash
dotnet run --project UserAuthAPI.Api
```

### 4. Access the API

- **API Base URL**: http://localhost:5098
- **Swagger Documentation**: http://localhost:5098/swagger
- **Health Check**: http://localhost:5098/api/health

## 🔑 Demo Accounts

The API automatically seeds demo accounts in Development mode:

| Email                    | Password        | Role          |
| ------------------------ | --------------- | ------------- |
| `admin@example.com`      | `Admin123!`     | Administrator |
| `demo@example.com`       | `Demo123!`      | Regular User  |
| `john.doe@example.com`   | `JohnDoe123!`   | Regular User  |
| `jane.smith@example.com` | `JaneSmith123!` | Regular User  |

## 📋 API Endpoints

### Authentication Endpoints

#### Register New User

```http
POST /api/auth/register
Content-Type: application/json

{
  "username": "newuser",
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

#### Login

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "demo@example.com",
  "password": "Demo123!"
}
```

**Response:**

```json
{
	"accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
	"refreshToken": "abc123...",
	"expiresAt": "2025-09-22T08:06:37Z",
	"user": {
		"id": "guid-here",
		"email": "demo@example.com",
		"firstName": "Demo",
		"lastName": "User"
	}
}
```

#### Refresh Token

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "your-refresh-token-here"
}
```

#### Logout

```http
POST /api/auth/logout
Authorization: Bearer your-jwt-token
Content-Type: application/json

{
  "refreshToken": "your-refresh-token-here"
}
```

### User Management Endpoints

#### Get Current User Profile

```http
GET /api/users/me
Authorization: Bearer your-jwt-token
```

#### Update User Profile

```http
PUT /api/users/me
Authorization: Bearer your-jwt-token
Content-Type: application/json

{
  "firstName": "UpdatedFirst",
  "lastName": "UpdatedLast"
}
```

#### Change Password

```http
POST /api/users/change-password
Authorization: Bearer your-jwt-token
Content-Type: application/json

{
  "currentPassword": "CurrentPass123!",
  "newPassword": "NewSecurePass123!"
}
```

### Development Endpoints (Development Only)

#### View Seeded Users

```http
GET /api/dev/seeded-users
```

#### Health Check

```http
GET /api/health
```

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Categories

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only
dotnet test --filter Category=Integration
```

### Test Coverage

The project includes comprehensive tests covering:

- **Authentication Service Tests** - Login, registration, token generation
- **Password Service Tests** - Hashing and validation
- **JWT Service Tests** - Token creation and validation
- **Repository Tests** - Data access layer
- **Controller Integration Tests** - End-to-end API testing
- **Validation Tests** - Input validation scenarios

## ⚙️ Configuration

### Environment Settings

#### Development (appsettings.Development.json)

- Database seeding enabled
- Detailed logging and error information
- Swagger UI enabled
- Extended CORS origins for local development
- Longer JWT token expiration (60 minutes)

#### Production (appsettings.Production.json)

- Database seeding disabled
- Minimal logging for performance
- Swagger UI disabled
- Strict CORS policy
- Short JWT token expiration (15 minutes)
- HTTPS enforcement

### Key Configuration Sections

#### JWT Settings

```json
{
	"Jwt": {
		"SecretKey": "your-secret-key-here",
		"Issuer": "UserAuthAPI",
		"Audience": "UserAuthAPI",
		"AccessTokenExpirationMinutes": 60,
		"RefreshTokenExpirationDays": 30
	}
}
```

#### Database Configuration

```json
{
	"ConnectionStrings": {
		"DefaultConnection": "Data Source=userauth-dev.db"
	},
	"Database": {
		"EnableSensitiveDataLogging": true,
		"EnableDetailedErrors": true
	}
}
```

#### Feature Toggles

```json
{
	"Features": {
		"EnableSwagger": true,
		"EnableDetailedErrors": true,
		"EnableRequestLogging": true,
		"EnableDatabaseSeeding": true
	}
}
```

## 🔧 Development Tools

### Database Management

```bash
# Add new migration
dotnet ef migrations add MigrationName --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api

# Update database
dotnet ef database update --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api

# Drop database (for fresh start)
dotnet ef database drop --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api
```

### Build and Run Commands

```bash
# Build solution
dotnet build

# Run with hot reload
dotnet watch run --project UserAuthAPI.Api

# Run tests with watch
dotnet watch test

# Generate test coverage report
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Logging

The API uses **Serilog** for structured logging with multiple outputs:

### Log Levels

- **Debug**: Detailed flow information (Development only)
- **Information**: General application flow
- **Warning**: Unexpected situations that don't stop the application
- **Error**: Error events that don't stop the application
- **Fatal**: Critical errors that cause application termination

### Log Outputs

- **Console**: Formatted output for development
- **File**: Rotating daily log files in `/logs` directory
- **Structured**: JSON format for log aggregation systems

### Sample Log Entry

```json
{
	"Timestamp": "2025-09-22T07:02:46.749Z",
	"Level": "Information",
	"Message": "User {UserId} logged in successfully",
	"Properties": {
		"UserId": "259463f1-35e4-4703-a68e-229dadd4d508",
		"Application": "UserAuthAPI"
	}
}
```

## 🔒 Security Features

### Password Security

- **BCrypt Hashing**: Industry-standard password hashing
- **Salt Rounds**: Configurable work factor for future-proofing
- **Password Validation**: Enforced complexity requirements

### JWT Security

- **Short-lived Access Tokens**: 15-60 minutes depending on environment
- **Refresh Token Rotation**: Secure token refresh mechanism
- **Token Validation**: Comprehensive token verification

### API Security

- **CORS Configuration**: Environment-specific origin policies
- **Security Headers**: X-Frame-Options, X-Content-Type-Options, etc.
- **HTTPS Enforcement**: Production-ready SSL/TLS configuration
- **Input Validation**: FluentValidation prevents injection attacks

## 🐛 Troubleshooting

### Common Issues

#### "Database does not exist"

```bash
dotnet ef database update --project UserAuthAPI.Infrastructure --startup-project UserAuthAPI.Api
```

#### "JWT Secret Key not configured"

- Check `appsettings.json` has `Jwt.SecretKey` configured
- Ensure the key is at least 32 characters long

#### "CORS policy violation"

- Add your frontend URL to `appsettings.Development.json` CORS origins
- Verify the request includes proper headers

#### "Seeding failed - UNIQUE constraint"

- Delete the database file and restart the application
- Check seeding data for duplicate emails/usernames

### Debug Mode

Enable detailed logging and error information:

```json
{
	"Database": {
		"EnableSensitiveDataLogging": true,
		"EnableDetailedErrors": true
	},
	"Features": {
		"EnableDetailedErrors": true
	}
}
```

## 🚢 Deployment

### Environment Variables (Production)

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443;http://+:80
JWT_SECRET_KEY=your-production-secret-key
DATABASE_CONNECTION_STRING=your-production-db-connection
```

### Docker Support (Future Enhancement)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["UserAuthAPI.Api/UserAuthAPI.Api.csproj", "UserAuthAPI.Api/"]
RUN dotnet restore "UserAuthAPI.Api/UserAuthAPI.Api.csproj"
COPY . .
WORKDIR "/src/UserAuthAPI.Api"
RUN dotnet build "UserAuthAPI.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserAuthAPI.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserAuthAPI.Api.dll"]
```

## 📖 Learning Resources

### Recommended Reading

- [Clean Architecture by Robert C. Martin](https://www.amazon.com/Clean-Architecture-Craftsmans-Software-Structure/dp/0134494164)
- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [JWT Best Practices](https://datatracker.ietf.org/doc/html/rfc8725)

### Next Steps for Learning

1. **Add Role-based Authorization** - Implement user roles and permissions
2. **Email Verification** - Add email confirmation workflow
3. **Password Reset** - Implement forgot password functionality
4. **API Rate Limiting** - Add request throttling
5. **Caching Layer** - Implement Redis for performance
6. **Microservices** - Split into multiple services
7. **Event Sourcing** - Implement audit trails

## 🤝 Contributing

This project is designed for educational purposes. Feel free to:

- Fork and experiment
- Submit issues for bugs or improvements
- Suggest additional best practices to demonstrate
- Create pull requests with educational enhancements

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For questions about this educational project:

- Review the comprehensive inline code comments
- Check the `/docs` folder for additional documentation
- Refer to the troubleshooting section above
- Open an issue for bugs or feature requests

---

**Built with ❤️ for learning C# and .NET development**

> This project demonstrates production-ready patterns while maintaining educational clarity. Each component is thoroughly documented and tested to serve as a learning resource for junior developers entering the C# ecosystem.
