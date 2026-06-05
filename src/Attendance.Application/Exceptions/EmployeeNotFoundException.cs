namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when an operation references an employee ID that does not exist in the system.
/// </summary>
public sealed class EmployeeNotFoundException : Exception
{
    /// <summary>Gets the ID that was searched for but not found.</summary>
    public int EmployeeId { get; }

    /// <summary>
    /// Initializes a new instance for the specified employee ID.
    /// </summary>
    /// <param name="employeeId">The ID that yielded no match in the database.</param>
    public EmployeeNotFoundException(int employeeId)
        : base($"Employee with ID {employeeId} was not found.")
    {
        EmployeeId = employeeId;
    }
}
