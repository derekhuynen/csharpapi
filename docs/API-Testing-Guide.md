# API Testing Guide

This guide provides comprehensive examples for testing the UserAuth API using various tools and scenarios.

## Quick Testing with cURL

### 1. Health Check

```bash
curl -X GET "http://localhost:5098/api/health"
```

**Expected Response:**

```json
{
	"status": "healthy",
	"timestamp": "2025-09-22T07:02:47Z",
	"version": "1.0.0",
	"environment": "Development"
}
```

### 2. View Seeded Users (Development Only)

```bash
curl -X GET "http://localhost:5098/api/dev/seeded-users"
```

### 3. User Registration

```bash
curl -X POST "http://localhost:5098/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "TestPass123!",
    "firstName": "Test",
    "lastName": "User"
  }'
```

### 4. User Login

```bash
curl -X POST "http://localhost:5098/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "demo@example.com",
    "password": "Demo123!"
  }'
```

**Save the access token from the response for authenticated requests:**

```bash
export ACCESS_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 5. Get User Profile (Authenticated)

```bash
curl -X GET "http://localhost:5098/api/users/me" \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

### 6. Update User Profile (Authenticated)

```bash
curl -X PUT "http://localhost:5098/api/users/me" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Updated",
    "lastName": "Name"
  }'
```

### 7. Change Password (Authenticated)

```bash
curl -X POST "http://localhost:5098/api/users/change-password" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "Demo123!",
    "newPassword": "NewSecurePass123!"
  }'
```

### 8. Refresh Token

```bash
# Save refresh token from login response
export REFRESH_TOKEN="kYasndSDF1Aa0oV38FyjMbUVSIU8AT5YWezKXuzwzvc="

curl -X POST "http://localhost:5098/api/auth/refresh" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "'$REFRESH_TOKEN'"
  }'
```

### 9. Logout (Authenticated)

```bash
curl -X POST "http://localhost:5098/api/auth/logout" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "'$REFRESH_TOKEN'"
  }'
```

## Testing with Postman

### Import Collection

Create a Postman collection with the following setup:

#### Environment Variables

```json
{
	"baseUrl": "http://localhost:5098",
	"accessToken": "",
	"refreshToken": ""
}
```

#### Pre-request Script for Authentication

Add this to requests requiring authentication:

```javascript
// Auto-set Authorization header
if (pm.environment.get('accessToken')) {
	pm.request.headers.add({
		key: 'Authorization',
		value: 'Bearer ' + pm.environment.get('accessToken'),
	});
}
```

#### Test Script for Login

Add this to the Login request to save tokens:

```javascript
// Save tokens after successful login
if (pm.response.code === 200) {
	const response = pm.response.json();
	pm.environment.set('accessToken', response.accessToken);
	pm.environment.set('refreshToken', response.refreshToken);
}
```

## Testing Scenarios

### Happy Path - Complete User Journey

1. **Register** → Create new account
2. **Login** → Get access token
3. **Get Profile** → Verify user data
4. **Update Profile** → Modify user information
5. **Change Password** → Update security credentials
6. **Refresh Token** → Extend session
7. **Logout** → Clean session termination

### Error Testing Scenarios

#### 1. Invalid Registration Data

```bash
# Missing required fields
curl -X POST "http://localhost:5098/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "invalid-email",
    "password": "weak"
  }'
```

**Expected Response (400 Bad Request):**

```json
{
	"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
	"title": "One or more validation errors occurred.",
	"status": 400,
	"errors": {
		"Username": ["The Username field is required."],
		"Email": ["The Email field is not a valid e-mail address."],
		"Password": ["Password must be at least 8 characters long."]
	}
}
```

#### 2. Invalid Login Credentials

```bash
curl -X POST "http://localhost:5098/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nonexistent@example.com",
    "password": "WrongPassword123!"
  }'
```

#### 3. Unauthorized Access

```bash
# Request without token
curl -X GET "http://localhost:5098/api/users/me"
```

#### 4. Expired Token

```bash
# Use an expired or invalid token
curl -X GET "http://localhost:5098/api/users/me" \
  -H "Authorization: Bearer invalid.token.here"
```

### Load Testing Scenarios

#### Simple Load Test with curl

```bash
# Test 100 concurrent health checks
for i in {1..100}; do
  curl -X GET "http://localhost:5098/api/health" &
done
wait
```

#### Authentication Load Test

```bash
# Test multiple concurrent logins
for i in {1..10}; do
  curl -X POST "http://localhost:5098/api/auth/login" \
    -H "Content-Type: application/json" \
    -d '{
      "email": "demo@example.com",
      "password": "Demo123!"
    }' &
done
wait
```

## Testing with HTTPie

### Installation

```bash
pip install httpie
```

### Basic Usage

```bash
# Health check
http GET localhost:5098/api/health

# Login
http POST localhost:5098/api/auth/login \
  email=demo@example.com \
  password=Demo123!

# Authenticated request
http GET localhost:5098/api/users/me \
  Authorization:"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

## Automated Testing Scripts

### Bash Testing Script

```bash
#!/bin/bash

BASE_URL="http://localhost:5098"
EMAIL="test$(date +%s)@example.com"
PASSWORD="TestPass123!"

echo "🧪 Running API Tests..."

# Test 1: Health Check
echo "✅ Testing health endpoint..."
curl -f "$BASE_URL/api/health" > /dev/null || exit 1

# Test 2: Registration
echo "✅ Testing user registration..."
REGISTER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"username\": \"testuser$(date +%s)\",
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\",
    \"firstName\": \"Test\",
    \"lastName\": \"User\"
  }")

# Test 3: Login
echo "✅ Testing user login..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\"
  }")

ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.accessToken')

# Test 4: Authenticated Request
echo "✅ Testing authenticated request..."
curl -f -X GET "$BASE_URL/api/users/me" \
  -H "Authorization: Bearer $ACCESS_TOKEN" > /dev/null || exit 1

echo "🎉 All tests passed!"
```

### PowerShell Testing Script

```powershell
$BaseUrl = "http://localhost:5098"
$Email = "test$(Get-Date -Format 'yyyyMMddHHmmss')@example.com"
$Password = "TestPass123!"

Write-Host "🧪 Running API Tests..." -ForegroundColor Green

# Test 1: Health Check
Write-Host "✅ Testing health endpoint..." -ForegroundColor Yellow
$healthResponse = Invoke-RestMethod -Uri "$BaseUrl/api/health" -Method GET

# Test 2: Registration
Write-Host "✅ Testing user registration..." -ForegroundColor Yellow
$registerBody = @{
    username = "testuser$(Get-Date -Format 'yyyyMMddHHmmss')"
    email = $Email
    password = $Password
    firstName = "Test"
    lastName = "User"
} | ConvertTo-Json

$registerResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json"

# Test 3: Login
Write-Host "✅ Testing user login..." -ForegroundColor Yellow
$loginBody = @{
    email = $Email
    password = $Password
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"

# Test 4: Authenticated Request
Write-Host "✅ Testing authenticated request..." -ForegroundColor Yellow
$headers = @{ Authorization = "Bearer $($loginResponse.accessToken)" }
$profileResponse = Invoke-RestMethod -Uri "$BaseUrl/api/users/me" -Method GET -Headers $headers

Write-Host "🎉 All tests passed!" -ForegroundColor Green
```

## Performance Testing

### Expected Response Times

- **Health Check**: < 50ms
- **User Registration**: < 200ms
- **User Login**: < 300ms (due to BCrypt hashing)
- **Authenticated Requests**: < 100ms
- **Database Operations**: < 150ms

### Memory Usage

- **Startup Memory**: ~50-80 MB
- **Under Load**: ~100-150 MB
- **Database Size**: ~1-5 MB (depending on user count)

## Troubleshooting Tests

### Common Test Failures

#### Connection Refused

```bash
curl: (7) Failed to connect to localhost port 5098: Connection refused
```

**Solution**: Ensure the API is running with `dotnet run --project UserAuthAPI.Api`

#### 401 Unauthorized

```json
{
	"type": "https://tools.ietf.org/html/rfc9110#section-15.5.2",
	"title": "Unauthorized",
	"status": 401
}
```

**Solution**: Include valid JWT token in Authorization header

#### 400 Bad Request with Validation Errors

**Solution**: Check request body format and required fields

### Debug Mode Testing

Enable detailed logging for troubleshooting:

```json
{
	"Serilog": {
		"MinimumLevel": {
			"Default": "Debug"
		}
	}
}
```

## Testing Best Practices

1. **Always test the happy path first** - Ensure basic functionality works
2. **Test edge cases** - Invalid data, missing fields, malformed requests
3. **Test authentication flows** - Login, logout, token refresh
4. **Test authorization** - Ensure protected endpoints require authentication
5. **Monitor performance** - Check response times and resource usage
6. **Clean up test data** - Remove test users and tokens after testing
7. **Use environment variables** - Don't hardcode sensitive data in scripts
8. **Test error scenarios** - Network failures, invalid tokens, etc.

---

_This testing guide provides comprehensive coverage of the UserAuth API functionality. Use these examples as starting points for your own testing scenarios._
