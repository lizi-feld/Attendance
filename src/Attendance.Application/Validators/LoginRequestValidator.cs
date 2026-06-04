using Attendance.Application.DTOs;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates the <see cref="LoginRequest"/> DTO before it reaches the authentication service.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Initializes the validation rules for <see cref="LoginRequest"/>.</summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9._\-]+$")
            .WithMessage("Username may only contain letters, digits, dots, hyphens, and underscores.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
}
