using Attendance.Application.Commands;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates the <see cref="ClockOutCommand"/> before the attendance service closes an active session.
/// </summary>
public sealed class ClockOutCommandValidator : AbstractValidator<ClockOutCommand>
{
    /// <summary>Initializes the validation rules for <see cref="ClockOutCommand"/>.</summary>
    public ClockOutCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("A valid employee ID is required.");
    }
}
