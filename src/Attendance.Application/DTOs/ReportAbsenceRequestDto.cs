using Attendance.Domain.Enums;

namespace Attendance.Application.DTOs;

/// <summary>
/// HTTP request body for <c>POST /api/attendance/report-absence</c>.
/// </summary>
public sealed record ReportAbsenceRequestDto
{
    /// <summary>Gets the calendar date the absence applies to.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Gets the category of absence being reported.</summary>
    public AbsenceType AbsenceType { get; init; }

    /// <summary>
    /// Gets the URL/path of a previously uploaded supporting document
    /// (see <c>POST /api/attendance/upload-document</c>).
    /// Required when <see cref="AbsenceType"/> is <see cref="Domain.Enums.AbsenceType.SickLeave"/>,
    /// <see cref="Domain.Enums.AbsenceType.ChildSickLeave"/>, or <see cref="Domain.Enums.AbsenceType.Other"/>.
    /// </summary>
    public string? DocumentUrl { get; init; }

    /// <summary>Gets an optional free-text note accompanying the absence report.</summary>
    public string? Note { get; init; }
}
