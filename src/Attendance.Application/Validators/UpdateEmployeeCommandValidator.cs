using Attendance.Application.Commands;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates the <see cref="UpdateEmployeeCommand"/> before the service layer applies the update.
/// All three payload fields are optional; rules are only enforced when a field is present.
/// </summary>
public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    /// <summary>Initializes the validation rules for <see cref="UpdateEmployeeCommand"/>.</summary>
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Employee ID must be a positive integer.");

        When(x => x.FullName is not null, () =>
        {
            RuleFor(x => x.FullName!)
                .NotEmpty().WithMessage("Full name cannot be empty.")
                .MaximumLength(200).WithMessage("Full name cannot exceed 200 characters.");
        });

        When(x => x.Username is not null, () =>
        {
            RuleFor(x => x.Username!)
                .NotEmpty().WithMessage("Username cannot be empty.")
                .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.")
                .Matches(@"^[a-zA-Z0-9._\-]+$")
                .WithMessage("Username may only contain letters, digits, dots, hyphens, and underscores.");
        });

        When(x => x.Password is not null, () =>
        {
            RuleFor(x => x.Password!)
                .NotEmpty().WithMessage("Password cannot be empty.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
                .MaximumLength(128).WithMessage("Password cannot exceed 128 characters.")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");
        });
    }
}
