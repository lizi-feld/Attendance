namespace Attendance.Application.Commands;

/// <summary>
/// Command to open a new attendance session for an employee.
/// The clock-in timestamp is sourced internally from <c>ITimeProvider</c> — it is not supplied by the caller.
/// </summary>
public sealed record ClockInCommand
{
    /// <summary>Gets the database identifier of the employee clocking in.</summary>
    public int EmployeeId { get; init; }
}
