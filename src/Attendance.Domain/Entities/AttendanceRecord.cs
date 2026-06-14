using Attendance.Domain.Exceptions;

namespace Attendance.Domain.Entities;

/// <summary>
/// Represents a single attendance session (clock-in to clock-out) for an employee.
/// All timestamps are sourced from the external time provider (Europe/Zurich timezone).
/// </summary>
public sealed class AttendanceRecord
{
    /// <summary>Required by Entity Framework Core — do not use directly.</summary>
    private AttendanceRecord() { }

    /// <summary>Gets the attendance record's unique database identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the foreign key referencing the associated <see cref="Employee"/>.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>
    /// Gets the timestamp when the employee clocked in.
    /// Sourced from the external time provider (Europe/Zurich).
    /// </summary>
    public DateTime ClockInTime { get; private set; }

    /// <summary>
    /// Gets the timestamp when the employee clocked out,
    /// or <c>null</c> if the session is still active.
    /// </summary>
    public DateTime? ClockOutTime { get; private set; }

    /// <summary>Gets the UTC timestamp when this record was inserted into the database.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the navigation property to the owning <see cref="Employee"/>.</summary>
    public Employee Employee { get; private set; } = null!;

    /// <summary>
    /// Gets the reason/note recorded when an administrator or employee manually adjusts
    /// this attendance record retroactively. <c>null</c> for regular clock-in/out records.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the attendance session is currently open (no clock-out recorded).
    /// </summary>
    public bool IsActive => ClockOutTime is null;

    /// <summary>
    /// Gets the total duration of the completed session,
    /// or <c>null</c> if the session is still active.
    /// </summary>
    public TimeSpan? Duration => ClockOutTime.HasValue
        ? ClockOutTime.Value - ClockInTime
        : null;

    /// <summary>
    /// Factory method that creates a new open attendance session for a clock-in event.
    /// </summary>
    /// <param name="employeeId">The ID of the employee clocking in.</param>
    /// <param name="clockInTime">Clock-in timestamp from the external time provider.</param>
    /// <param name="createdAt">UTC timestamp for database auditing.</param>
    /// <returns>A valid, unsaved <see cref="AttendanceRecord"/> with no clock-out.</returns>
    /// <exception cref="DomainException">Thrown when <paramref name="employeeId"/> is not positive.</exception>
    public static AttendanceRecord Create(int employeeId, DateTime clockInTime, DateTime createdAt)
    {
        if (employeeId <= 0)
            throw new DomainException("Employee ID must be a positive integer.");

        return new AttendanceRecord
        {
            EmployeeId = employeeId,
            ClockInTime = clockInTime,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Factory method that creates a fully completed manual attendance record (clock-in AND clock-out
    /// already known) for historical back-filling.
    /// </summary>
    /// <param name="employeeId">The ID of the employee the record belongs to.</param>
    /// <param name="clockInTime">Clock-in timestamp supplied by the actor.</param>
    /// <param name="clockOutTime">Clock-out timestamp; must be after <paramref name="clockInTime"/>.</param>
    /// <param name="note">Mandatory audit reason; must not be empty.</param>
    /// <param name="createdAt">UTC wall-clock timestamp for database auditing.</param>
    /// <returns>A valid, unsaved <see cref="AttendanceRecord"/> with both timestamps set.</returns>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="employeeId"/> is not positive, or
    /// <paramref name="clockOutTime"/> is not after <paramref name="clockInTime"/>.
    /// </exception>
    public static AttendanceRecord CreateManual(
        int employeeId,
        DateTime clockInTime,
        DateTime clockOutTime,
        string note,
        DateTime createdAt)
    {
        if (employeeId <= 0)
            throw new DomainException("Employee ID must be a positive integer.");

        if (clockOutTime <= clockInTime)
            throw new DomainException("Clock-out time must be after clock-in time.");

        return new AttendanceRecord
        {
            EmployeeId   = employeeId,
            ClockInTime  = clockInTime,
            ClockOutTime = clockOutTime,
            Note         = note,
            CreatedAt    = createdAt
        };
    }

    /// <summary>
    /// Retroactively adjusts the clock-in and clock-out times and records the mandatory reason note.
    /// </summary>
    /// <param name="newClockInTime">Replacement clock-in timestamp.</param>
    /// <param name="newClockOutTime">Replacement clock-out timestamp; must be after <paramref name="newClockInTime"/>.</param>
    /// <param name="note">Mandatory reason for the manual change; must not be empty.</param>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="newClockOutTime"/> is not after <paramref name="newClockInTime"/>.
    /// </exception>
    public void ManualUpdate(DateTime newClockInTime, DateTime newClockOutTime, string note)
    {
        if (newClockOutTime <= newClockInTime)
            throw new DomainException("Clock-out time must be after clock-in time.");

        ClockInTime  = newClockInTime;
        ClockOutTime = newClockOutTime;
        Note         = note;
    }

    /// <summary>
    /// Closes the attendance session by recording the clock-out timestamp.
    /// </summary>
    /// <param name="clockOutTime">Clock-out timestamp from the external time provider.</param>
    /// <exception cref="DomainException">
    /// Thrown when the session is already closed, or when
    /// <paramref name="clockOutTime"/> is not after <see cref="ClockInTime"/>.
    /// </exception>
    public void ClockOut(DateTime clockOutTime)
    {
        if (!IsActive)
            throw new DomainException("Attendance session is already closed.");

        if (clockOutTime <= ClockInTime)
            throw new DomainException("Clock-out time must be after clock-in time.");

        ClockOutTime = clockOutTime;
    }
}
