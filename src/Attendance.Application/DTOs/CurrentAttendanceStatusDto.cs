namespace Attendance.Application.DTOs;

/// <summary>
/// Represents the real-time attendance status for a specific employee.
/// Duration is calculated using the external time provider — never local server time.
/// </summary>
public sealed record CurrentAttendanceStatusDto
{
    /// <summary>Gets whether the employee currently has an open attendance session.</summary>
    public bool IsClockedIn { get; init; }

    /// <summary>
    /// Gets the primary key of the active <c>AttendanceRecord</c>,
    /// or <c>null</c> when the employee is not clocked in.
    /// </summary>
    public int? ActiveRecordId { get; init; }

    /// <summary>
    /// Gets the Europe/Zurich timestamp at which the active session started,
    /// or <c>null</c> when the employee is not clocked in.
    /// </summary>
    public DateTime? ClockInTime { get; init; }

    /// <summary>
    /// Gets the elapsed time since clock-in as of the moment this DTO was generated,
    /// or <c>null</c> when the employee is not clocked in.
    /// Calculated as: <c>ITimeProvider.GetCurrentTimeAsync() - ClockInTime</c>.
    /// </summary>
    public TimeSpan? CurrentDuration { get; init; }
}
