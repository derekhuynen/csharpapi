using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using UserAuthAPI.Application.DTOs;
using UserAuthAPI.Application.Interfaces;
using UserAuthAPI.Domain.Entities;

namespace UserAuthAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Authentication endpoints for user registration, login, token management")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshTokenValidator;

    public AuthController(
        IAuthService authService,
        IUserRepository userRepository,
        ILogger<AuthController> logger,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshTokenValidator)
    {
        _authService = authService;
        _userRepository = userRepository;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <returns>Authentication response with JWT tokens</returns>
    /// <remarks>
    /// Creates a new user account with the provided details. Password must meet complexity requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter  
    /// - At least one digit
    /// - At least one special character
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///        "email": "user@example.com",
    ///        "password": "StrongPass123!",
    ///        "firstName": "John",
    ///        "lastName": "Doe"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Registration successful - returns JWT tokens and user information</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="409">User with email already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register new user",
        Description = "Creates a new user account and returns JWT authentication tokens",
        OperationId = "RegisterUser",
        Tags = new[] { "Authentication" }
    )]
    [SwaggerResponse(200, "Registration successful", typeof(AuthResponse))]
    [SwaggerResponse(400, "Validation failed or bad request", typeof(object))]
    [SwaggerResponse(409, "User already exists", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _registerValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(e => new { Property = e.PropertyName, Message = e.ErrorMessage })
                });
            }

            _logger.LogInformation("User registration attempt for email: {Email}", request.Email);

            // Check if user already exists
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed - user already exists: {Email}", request.Email);
                return BadRequest(new { message = "User with this email already exists" });
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _authService.RegisterAsync(user, request.Password);

            if (!result.IsSuccess)
            {
                _logger.LogError("Registration failed for {Email}: {Message}", request.Email, result.ErrorMessage);
                return BadRequest(new { message = result.ErrorMessage });
            }

            _logger.LogInformation("User registered successfully: {Email}", request.Email);

            var response = new AuthResponse
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT tokens</returns>
    /// <remarks>
    /// Authenticates a user with email and password, returning JWT tokens for subsequent API calls.
    /// 
    /// Sample request:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///        "email": "user@example.com",
    ///        "password": "StrongPass123!"
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Login successful - returns JWT tokens and user information</response>
    /// <response code="400">Invalid request data or validation errors</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "User login",
        Description = "Authenticates user credentials and returns JWT tokens",
        OperationId = "LoginUser",
        Tags = new[] { "Authentication" }
    )]
    [SwaggerResponse(200, "Login successful", typeof(AuthResponse))]
    [SwaggerResponse(400, "Validation failed", typeof(object))]
    [SwaggerResponse(401, "Invalid credentials", typeof(object))]
    [SwaggerResponse(500, "Internal server error", typeof(object))]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _loginValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(e => new { Property = e.PropertyName, Message = e.ErrorMessage })
                });
            }

            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Login failed for {Email}: {Message}", request.Email, result.ErrorMessage);
                return Unauthorized(new { message = result.ErrorMessage });
            }

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogError("User not found after successful login: {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);

            var response = new AuthResponse
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New authentication response with fresh JWT tokens</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _refreshTokenValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(e => new { Property = e.PropertyName, Message = e.ErrorMessage })
                });
            }

            _logger.LogInformation("Token refresh attempt");

            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Token refresh failed: {Message}", result.ErrorMessage);
                return Unauthorized(new { message = result.ErrorMessage });
            }

            var user = await _userRepository.GetByIdAsync(result.UserId);
            if (user == null)
            {
                _logger.LogError("User not found during token refresh: {UserId}", result.UserId);
                return StatusCode(500, new { message = "An error occurred during token refresh" });
            }

            _logger.LogInformation("Token refreshed successfully for user: {UserId}", result.UserId);

            var response = new AuthResponse
            {
                AccessToken = result.AccessToken!,
                RefreshToken = result.RefreshToken!,
                ExpiresAt = result.ExpiresAt,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Logout and invalidate refresh token
    /// </summary>
    /// <param name="request">Refresh token to invalidate</param>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Logout attempt for user: {UserId}", userId);

            await _authService.LogoutAsync(request.RefreshToken);

            _logger.LogInformation("User logged out successfully: {UserId}", userId);
            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Current user not found: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID in token");
        }
        return userId;
    }
}