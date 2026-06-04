namespace Attendance.Application.Queries;

/// <summary>
/// Query to retrieve the full attendance history for a specific employee,
/// ordered by clock-in time descending.
/// </summary>
/// <param name="EmployeeId">The unique identifier of the employee whose records are requested.</param>
public sealed record GetAttendanceRecordsQuery(int EmployeeId);
