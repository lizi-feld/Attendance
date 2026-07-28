namespace Attendance.Application.DTOs;

/// <summary>
/// Represents a single reported absence for API consumption.
/// </summary>
public sealed record AbsenceRecordDto
{
    /// <summary>Gets the record's unique database identifier.</summary>
    public int Id { get; init; }

    /// <summary>Gets the associated employee's database identifier.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Gets the associated employee's full display name.</summary>
    public string EmployeeFullName { get; init; } = string.Empty;

    /// <summary>Gets the calendar date the absence applies to.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Gets the category of absence as a human-readable string (e.g. <c>"SickLeave"</c>).</summary>
    public string AbsenceType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the URL/path of the uploaded supporting document,
    /// or <c>null</c> when the absence type did not require one.
    /// </summary>
    public string? DocumentUrl { get; init; }

    /// <summary>Gets the optional free-text note accompanying the absence report.</summary>
    public string? Note { get; init; }

    /// <summary>Gets the UTC timestamp when this record was inserted into the database.</summary>
    public DateTime CreatedAt { get; init; }
}
