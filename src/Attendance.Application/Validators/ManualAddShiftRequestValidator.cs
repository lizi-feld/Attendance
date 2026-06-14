using Attendance.Application.DTOs;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates <see cref="ManualAddShiftRequestDto"/>.
/// The <c>Note</c> field is REQUIRED — this mirrors the audit-trail rule from
/// <see cref="ManualTimeUpdateRequestValidator"/> and is intentionally absent from
/// regular clock-in/out validators.
/// </summary>
public sealed class ManualAddShiftRequestValidator : AbstractValidator<ManualAddShiftRequestDto>
{
    /// <summary>Initializes validation rules for manual shift creation.</summary>
    public ManualAddShiftRequestValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason note is required for manually added attendance records.")
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.");

        RuleFor(x => x.ClockOutTime)
            .GreaterThan(x => x.ClockInTime)
            .WithMessage("Clock-out time must be after clock-in time.");
    }
}
