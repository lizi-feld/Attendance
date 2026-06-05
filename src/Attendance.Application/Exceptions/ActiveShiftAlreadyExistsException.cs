namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when a clock-in is attempted for an employee who already has an open attendance session.
/// Business rule: an employee may have at most one active shift at any time.
/// </summary>
public sealed class ActiveShiftAlreadyExistsException : Exception
{
    /// <summary>Gets the ID of the employee who triggered the violation.</summary>
    public int EmployeeId { get; }

    /// <summary>Gets the ID of the existing active attendance record.</summary>
    public int ActiveRecordId { get; }

    /// <summary>
    /// Initializes a new instance with the IDs of the conflicting employee and record.
    /// </summary>
    /// <param name="employeeId">The employee who attempted to clock in again.</param>
    /// <param name="activeRecordId">The existing open attendance record for that employee.</param>
    public ActiveShiftAlreadyExistsException(int employeeId, int activeRecordId)
        : base($"Employee {employeeId} already has an active shift (record ID: {activeRecordId}). Clock out first.")
    {
        EmployeeId = employeeId;
        ActiveRecordId = activeRecordId;
    }
}
