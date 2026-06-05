using Attendance.Application.DTOs;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the business operations for managing employee attendance.
/// <para>
/// All clock-in/out timestamps are fetched exclusively from
/// <see cref="ITimeProvider"/> (Europe/Zurich). Local machine time is never used.
/// </para>
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Records a clock-in event for the specified employee using the external time provider.
    /// </summary>
    /// <param name="employeeId">The ID of the employee clocking in.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created <see cref="AttendanceRecordDto"/> for the open session.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the employee already has an active (open) session.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<AttendanceRecordDto> ClockInAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a clock-out event for the employee's current active session using the external time provider.
    /// </summary>
    /// <param name="employeeId">The ID of the employee clocking out.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The updated <see cref="AttendanceRecordDto"/> with clock-out time and total duration populated.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the employee has no open session to close.
    /// </exception>
    /// <exception cref="KeyNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<AttendanceRecordDto> ClockOutAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete attendance history for a specific employee.
    /// </summary>
    /// <param name="employeeId">The ID of the employee.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="AttendanceRecordDto"/> entries ordered by clock-in time descending.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<IReadOnlyList<AttendanceRecordDto>> GetRecordsAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the real-time attendance status for a specific employee,
    /// including elapsed duration calculated from the external time provider.
    /// </summary>
    /// <param name="employeeId">The ID of the employee to query.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="CurrentAttendanceStatusDto"/> indicating whether the employee is clocked in
    /// and, if so, the active record details and current elapsed time.
    /// </returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<CurrentAttendanceStatusDto> GetCurrentStatusAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated attendance history for a specific employee,
    /// ordered by clock-in time descending.
    /// </summary>
    /// <param name="employeeId">The ID of the employee.</param>
    /// <param name="pageNumber">The 1-based page number to retrieve.</param>
    /// <param name="pageSize">The number of records per page (max recommended: 100).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="PagedResult{T}"/> of <see cref="AttendanceRecordDto"/> entries.</returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<PagedResult<AttendanceRecordDto>> GetAttendanceHistoryAsync(
        int employeeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total worked hours for the current ISO week (Monday 00:00 – Sunday 23:59:59)
    /// using the external time provider to determine "today".
    /// Active sessions contribute their elapsed time up to the current moment.
    /// </summary>
    /// <param name="employeeId">The ID of the employee.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The total <see cref="TimeSpan"/> worked this week.</returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<TimeSpan> GetWeeklyHoursAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total worked hours for the current calendar month
    /// using the external time provider to determine "today".
    /// Active sessions contribute their elapsed time up to the current moment.
    /// </summary>
    /// <param name="employeeId">The ID of the employee.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The total <see cref="TimeSpan"/> worked this month.</returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<TimeSpan> GetMonthlyHoursAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a real-time dashboard summary including active sessions and today's statistics.
    /// The current date is determined via the external time provider (Europe/Zurich).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="DashboardSummaryDto"/> snapshot of the current attendance state.</returns>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}
