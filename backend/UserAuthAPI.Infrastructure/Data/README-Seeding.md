# Database Seeding Documentation

## Overview

The UserAuth API includes a comprehensive database seeding system that automatically populates the database with initial data for development and testing purposes.

## How It Works

### 1. DatabaseSeeder Class

- **Location**: `UserAuthAPI.Infrastructure.Data.DatabaseSeeder`
- **Purpose**: Handles the creation of initial user data
- **Safety**: Only seeds if the database is empty (checks for existing users)

### 2. Seeding Configuration

- **Development**: Seeding is **enabled** by default (`EnableDatabaseSeeding: true`)
- **Production**: Seeding is **disabled** by default (`EnableDatabaseSeeding: false`)
- **Environment Check**: Only runs in Development environment unless explicitly configured

### 3. Seeded Data

The seeding process creates 4 demo users:

| Username     | Email                  | Password        | Role          |
| ------------ | ---------------------- | --------------- | ------------- |
| `admin`      | admin@example.com      | `Admin123!`     | Administrator |
| `john.doe`   | john.doe@example.com   | `JohnDoe123!`   | Regular User  |
| `jane.smith` | jane.smith@example.com | `JaneSmith123!` | Regular User  |
| `demo`       | demo@example.com       | `Demo123!`      | Demo User     |

### 4. Security Features

- All passwords are properly hashed using BCrypt
- Unique constraints are respected (Username and Email)
- Only creates data if database is empty
- Comprehensive error handling and logging

## Configuration

### Enabling/Disabling Seeding

**appsettings.Development.json**:

```json
{
	"Features": {
		"EnableDatabaseSeeding": true
	}
}
```

**appsettings.Production.json**:

```json
{
	"Features": {
		"EnableDatabaseSeeding": false
	}
}
```

### Environment Variables

You can also control seeding via environment variables:

```bash
ASPNETCORE_ENVIRONMENT=Development  # Enables seeding when EnableDatabaseSeeding is true
```

## Usage

### Automatic Seeding

Seeding happens automatically when the application starts in Development mode with a fresh database.

### Manual Database Reset

To trigger fresh seeding:

1. Stop the API
2. Delete the database file:
   ```bash
   rm UserAuthAPI.Api/userauth-dev.db
   ```
3. Start the API - seeding will occur automatically

### Logs

Watch the application logs for seeding status:

```
[INF] Starting database seeding...
[INF] Seeding users...
[INF] Added 4 users to the database.
[INF] Database seeding completed successfully.
```

Or if data already exists:

```
[INF] Database already contains data. Skipping seeding.
```

## Testing with Seeded Data

After seeding completes, you can test the API with the demo accounts:

### Login Example

```bash
curl -X POST "http://localhost:5098/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "demo",
    "password": "Demo123!"
  }'
```

### Response

```json
{
	"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
	"refreshToken": "abc123...",
	"expiresAt": "2025-09-22T01:02:47Z",
	"user": {
		"id": "guid-here",
		"username": "demo",
		"email": "demo@example.com",
		"firstName": "Demo",
		"lastName": "User"
	}
}
```

## Best Practices

1. **Never enable seeding in production** - Use proper data migration strategies
2. **Use environment-specific configuration** - Keep development and production settings separate
3. **Regular password updates** - Change demo passwords for public demonstrations
4. **Data privacy** - Don't include real user data in seeding scripts
5. **Testing isolation** - Each test should use its own database context

## Troubleshooting

### Common Issues

**"UNIQUE constraint failed"**

- Means data already exists in the database
- Delete database file or check seeding logic

**"Seeding is disabled"**

- Check `Features.EnableDatabaseSeeding` in appsettings
- Verify environment is Development

**"Database connection failed"**

- Check connection string in appsettings
- Ensure database file permissions are correct

### Debug Mode

Enable detailed EF Core logging in Development:

```json
{
	"Database": {
		"EnableSensitiveDataLogging": true,
		"EnableDetailedErrors": true
	}
}
```

## Extending Seeding

To add more seed data:

1. Create new methods in `DatabaseSeeder`
2. Call them from `SeedAsync()`
3. Follow the same pattern: check if data exists, create if not
4. Use proper error handling and logging

Example:

```csharp
private async Task SeedRolesAsync()
{
    if (await _context.Roles.AnyAsync()) return;

    var roles = new List<Role>
    {
        new Role { Name = "Administrator" },
        new Role { Name = "User" }
    };

    await _context.Roles.AddRangeAsync(roles);
    _logger.LogInformation("Added {RoleCount} roles to the database.", roles.Count);
}
```
