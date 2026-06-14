namespace Attendance.Application.DTOs;

/// <summary>
/// Represents a single attendance session for API consumption.
/// All timestamps reflect Europe/Zurich time as sourced from the external time provider.
/// </summary>
public sealed record AttendanceRecordDto
{
    /// <summary>Gets the record's unique database identifier.</summary>
    public int Id { get; init; }

    /// <summary>Gets the associated employee's database identifier.</summary>
    public int EmployeeId { get; init; }

    /// <summary>Gets the associated employee's full display name.</summary>
    public string EmployeeFullName { get; init; } = string.Empty;

    /// <summary>Gets the clock-in timestamp (Europe/Zurich time).</summary>
    public DateTime ClockInTime { get; init; }

    /// <summary>
    /// Gets the clock-out timestamp (Europe/Zurich time),
    /// or <c>null</c> when the session is still active.
    /// </summary>
    public DateTime? ClockOutTime { get; init; }

    /// <summary>
    /// Gets the total duration of the completed session,
    /// or <c>null</c> when the session is still active.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Gets whether this session is currently open (no clock-out recorded).</summary>
    public bool IsActive => ClockOutTime is null;

    /// <summary>Gets the UTC timestamp when this record was inserted into the database.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the reason note recorded for a manual/retroactive update,
    /// or <c>null</c> for records created by regular clock-in/out.
    /// </summary>
    public string? Note { get; init; }
}
