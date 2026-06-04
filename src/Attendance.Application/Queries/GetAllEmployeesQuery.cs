namespace Attendance.Application.Queries;

/// <summary>
/// Query to retrieve the summary list of all registered employees.
/// Restricted to administrators. Returns lightweight <c>EmployeeDto</c> projections.
/// </summary>
public sealed record GetAllEmployeesQuery;
