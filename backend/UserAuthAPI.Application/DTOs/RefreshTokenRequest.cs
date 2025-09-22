using System.ComponentModel.DataAnnotations;

namespace UserAuthAPI.Application.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}