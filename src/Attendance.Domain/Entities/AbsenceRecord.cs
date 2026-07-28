using Attendance.Domain.Enums;
using Attendance.Domain.Exceptions;

namespace Attendance.Domain.Entities;

/// <summary>
/// Represents a single reported absence (vacation, sick leave, holiday, etc.) for an employee.
/// Distinct from <see cref="AttendanceRecord"/>, which tracks worked clock-in/out sessions —
/// an absence has no clock-in/out and is keyed by calendar date rather than a timestamp range.
/// </summary>
public sealed class AbsenceRecord
{
    /// <summary>Required by Entity Framework Core — do not use directly.</summary>
    private AbsenceRecord() { }

    /// <summary>Gets the absence record's unique database identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the foreign key referencing the associated <see cref="Employee"/>.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>Gets the calendar date the absence applies to.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Gets the category of absence being reported.</summary>
    public AbsenceType Type { get; private set; }

    /// <summary>
    /// Gets the URL/path of the uploaded supporting document,
    /// or <c>null</c> when the absence type does not require one.
    /// </summary>
    public string? DocumentUrl { get; private set; }

    /// <summary>Gets the optional free-text note accompanying the absence report.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets the UTC timestamp when this record was inserted into the database.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the navigation property to the owning <see cref="Employee"/>.</summary>
    public Employee Employee { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether <paramref name="type"/> requires a supporting document
    /// (e.g. a doctor's note) to be attached.
    /// </summary>
    public static bool RequiresDocument(AbsenceType type) =>
        type is AbsenceType.SickLeave or AbsenceType.ChildSickLeave or AbsenceType.Other or AbsenceType.Pregnancy;

    /// <summary>
    /// Factory method that creates and validates a new <see cref="AbsenceRecord"/> instance.
    /// </summary>
    /// <param name="employeeId">The ID of the employee the absence belongs to.</param>
    /// <param name="date">The calendar date the absence applies to.</param>
    /// <param name="type">The category of absence.</param>
    /// <param name="documentUrl">The uploaded supporting document URL, or <c>null</c> if not required.</param>
    /// <param name="note">Optional free-text note (max 500 chars).</param>
    /// <param name="createdAt">Timestamp from the external time provider (Asia/Jerusalem).</param>
    /// <returns>A valid, unsaved <see cref="AbsenceRecord"/> entity.</returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="employeeId"/> is not positive, when <paramref name="type"/>
    /// requires a document but <paramref name="documentUrl"/> is missing, or when
    /// <paramref name="note"/> exceeds 500 characters.
    /// </exception>
    public static AbsenceRecord Create(
        int employeeId,
        DateOnly date,
        AbsenceType type,
        string? documentUrl,
        string? note,
        DateTime createdAt)
    {
        if (employeeId <= 0)
            throw new DomainException("Employee ID must be a positive integer.");

        if (RequiresDocument(type) && string.IsNullOrWhiteSpace(documentUrl))
            throw new DomainException($"A supporting document is required for absence type '{type}'.");

        if (note is { Length: > 500 })
            throw new DomainException("Note cannot exceed 500 characters.");

        return new AbsenceRecord
        {
            EmployeeId  = employeeId,
            Date        = date,
            Type        = type,
            DocumentUrl = string.IsNullOrWhiteSpace(documentUrl) ? null : documentUrl.Trim(),
            Note        = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt   = createdAt
        };
    }
}
