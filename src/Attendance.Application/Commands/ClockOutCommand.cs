namespace Attendance.Application.Commands;

/// <summary>
/// Command to close the active attendance session for an employee.
/// The clock-out timestamp is sourced internally from <c>ITimeProvider</c> — it is not supplied by the caller.
/// </summary>
public sealed record ClockOutCommand
{
    /// <summary>Gets the database identifier of the employee clocking out.</summary>
    public int EmployeeId { get; init; }
}
