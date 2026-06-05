namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when a clock-out is attempted for an employee who has no open attendance session.
/// Business rule: an employee must be clocked in before they can clock out.
/// </summary>
public sealed class ActiveShiftNotFoundException : Exception
{
    /// <summary>Gets the ID of the employee whose active shift was not found.</summary>
    public int EmployeeId { get; }

    /// <summary>
    /// Initializes a new instance for the specified employee.
    /// </summary>
    /// <param name="employeeId">The employee who attempted to clock out without an active session.</param>
    public ActiveShiftNotFoundException(int employeeId)
        : base($"No active shift found for employee {employeeId}. Clock in first.")
    {
        EmployeeId = employeeId;
    }
}
