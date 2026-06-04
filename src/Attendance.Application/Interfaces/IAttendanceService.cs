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
    /// Generates a real-time dashboard summary including active sessions and today's statistics.
    /// The current date is determined via the external time provider (Europe/Zurich).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="DashboardSummaryDto"/> snapshot of the current attendance state.</returns>
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}
