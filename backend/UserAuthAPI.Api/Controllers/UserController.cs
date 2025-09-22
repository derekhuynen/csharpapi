using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserAuthAPI.Application.DTOs;
using UserAuthAPI.Application.Interfaces;

namespace UserAuthAPI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<UserController> _logger;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public UserController(
        IUserRepository userRepository,
        IPasswordService passwordService,
        ILogger<UserController> logger,
        IValidator<UpdateProfileRequest> updateProfileValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _userRepository = userRepository;
        _passwordService = passwordService;
        _logger = logger;
        _updateProfileValidator = updateProfileValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    /// <summary>
    /// Get all users (admin functionality)
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Getting all users");

            var users = await _userRepository.GetAllAsync();
            var userDtos = users.Select(user => new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            }).ToList();

            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> GetUserById(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting user by ID: {UserId}", id);

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
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
            _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving user" });
        }
    }

    /// <summary>
    /// Update current user profile
    /// </summary>
    /// <param name="request">Updated user information</param>
    /// <returns>Updated user details</returns>
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _updateProfileValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(e => new { Property = e.PropertyName, Message = e.ErrorMessage })
                });
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Updating profile for user: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found during profile update: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            // Update user properties
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);

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
            _logger.LogError(ex, "Error updating profile for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "An error occurred while updating profile" });
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <returns>Success message</returns>
    [HttpPut("password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Validate request
            var validationResult = await _changePasswordValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors.Select(e => new { Property = e.PropertyName, Message = e.ErrorMessage })
                });
            }

            var userId = GetCurrentUserId();
            _logger.LogInformation("Password change attempt for user: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found during password change: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            // Verify current password
            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for user: {UserId}", userId);
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Hash new password and update
            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "An error occurred while changing password" });
        }
    }

    /// <summary>
    /// Deactivate current user account
    /// </summary>
    /// <returns>Success message</returns>
    [HttpDelete("account")]
    public async Task<ActionResult> DeactivateAccount()
    {
        try
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("Account deactivation attempt for user: {UserId}", userId);

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found during account deactivation: {UserId}", userId);
                return NotFound(new { message = "User not found" });
            }

            // Deactivate user account
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            _logger.LogInformation("Account deactivated successfully for user: {UserId}", userId);
            return Ok(new { message = "Account deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account for user: {UserId}", GetCurrentUserId());
            return StatusCode(500, new { message = "An error occurred while deactivating account" });
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