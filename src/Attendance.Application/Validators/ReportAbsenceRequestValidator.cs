using Attendance.Application.DTOs;
using Attendance.Domain.Enums;
using FluentValidation;

namespace Attendance.Application.Validators;

/// <summary>
/// Validates <see cref="ReportAbsenceRequestDto"/>.
/// </summary>
/// <remarks>
/// <see cref="AbsenceType.SickLeave"/>, <see cref="AbsenceType.ChildSickLeave"/>,
/// <see cref="AbsenceType.Other"/>, and <see cref="AbsenceType.Pregnancy"/> require a
/// non-empty <c>DocumentUrl</c>. <see cref="AbsenceType.Holiday"/>,
/// <see cref="AbsenceType.CholHaMoed"/>, and <see cref="AbsenceType.Vacation"/> do not.
/// </remarks>
public sealed class ReportAbsenceRequestValidator : AbstractValidator<ReportAbsenceRequestDto>
{
    private static readonly AbsenceType[] DocumentRequiredTypes =
    [
        AbsenceType.SickLeave,
        AbsenceType.ChildSickLeave,
        AbsenceType.Other,
        AbsenceType.Pregnancy
    ];

    /// <summary>Initializes the validation rules for <see cref="ReportAbsenceRequestDto"/>.</summary>
    public ReportAbsenceRequestValidator()
    {
        RuleFor(x => x.Date)
            .NotEqual(default(DateOnly)).WithMessage("A valid absence date is required.");

        RuleFor(x => x.AbsenceType)
            .IsInEnum().WithMessage("Absence type must be a valid, recognised value.");

        RuleFor(x => x.DocumentUrl)
            .NotEmpty()
            .WithMessage("A supporting document is required for this absence type.")
            .When(x => DocumentRequiredTypes.Contains(x.AbsenceType));

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.");
    }
}
