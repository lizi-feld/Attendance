using Attendance.Application.DTOs;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates <see cref="ManualTimeUpdateRequestDto"/>.
/// The <c>Note</c> field is REQUIRED here — this rule is intentionally absent from
/// regular clock-in/out validators because the note is only mandatory for manual updates.
/// </summary>
public sealed class ManualTimeUpdateRequestValidator : AbstractValidator<ManualTimeUpdateRequestDto>
{
    /// <summary>Initializes validation rules for manual attendance updates.</summary>
    public ManualTimeUpdateRequestValidator()
    {
        RuleFor(x => x.RecordId)
            .GreaterThan(0).WithMessage("A valid record ID is required.");

        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A reason note is required for manual attendance updates.")
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.");

        RuleFor(x => x.NewClockOutTime)
            .GreaterThan(x => x.NewClockInTime)
            .WithMessage("Clock-out time must be after clock-in time.");
    }
}
