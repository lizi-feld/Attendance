using Attendance.Application.Commands;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates the <see cref="ClockInCommand"/> before the attendance service processes the clock-in event.
/// </summary>
public sealed class ClockInCommandValidator : AbstractValidator<ClockInCommand>
{
    /// <summary>Initializes the validation rules for <see cref="ClockInCommand"/>.</summary>
    public ClockInCommandValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("A valid employee ID is required.");
    }
}
