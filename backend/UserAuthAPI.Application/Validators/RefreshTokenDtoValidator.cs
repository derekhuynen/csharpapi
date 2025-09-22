using FluentValidation;
using UserAuthAPI.Application.DTOs;

namespace UserAuthAPI.Application.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .MinimumLength(32).WithMessage("Invalid refresh token format")
            .MaximumLength(256).WithMessage("Invalid refresh token format");
    }
}