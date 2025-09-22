using System.ComponentModel.DataAnnotations;

namespace UserAuthAPI.Application.DTOs;

public class UpdateProfileRequest
{
    [Required]
    [MinLength(2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string LastName { get; set; } = string.Empty;
}