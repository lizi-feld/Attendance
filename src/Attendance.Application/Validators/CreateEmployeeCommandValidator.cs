using Attendance.Application.Commands;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates the <see cref="CreateEmployeeCommand"/> before the service layer creates a new employee.
/// </summary>
public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    /// <summary>Initializes the validation rules for <see cref="CreateEmployeeCommand"/>.</summary>
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9._\-]+$")
            .WithMessage("Username may only contain letters, digits, dots, hyphens, and underscores.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Role must be a valid system role (Employee or Admin).");
    }
}
