namespace Attendance.Application.Queries;

/// <summary>
/// Query to retrieve a single employee's full profile, including their complete attendance history.
/// Resolved by the employee service and restricted to admins or the employee themselves.
/// </summary>
/// <param name="EmployeeId">The unique identifier of the employee to retrieve.</param>
public sealed record GetEmployeeByIdQuery(int EmployeeId);
