using Attendance.Domain.Entities;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the data access contract for <see cref="AttendanceRecord"/> operations.
/// Implementations live in the Infrastructure layer and are injected via DI.
/// </summary>
public interface IAttendanceRepository
{
    /// <summary>
    /// Retrieves a single attendance record by its unique database identifier.
    /// </summary>
    /// <param name="id">The record's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>The matching <see cref="AttendanceRecord"/>, or <c>null</c> if not found.</returns>
    Task<AttendanceRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the currently open attendance session for an employee.
    /// An open session has no <c>ClockOutTime</c>.
    /// </summary>
    /// <param name="employeeId">The employee's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>The active <see cref="AttendanceRecord"/>, or <c>null</c> if none exists.</returns>
    Task<AttendanceRecord?> GetActiveRecordAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all attendance records for a specific employee,
    /// ordered by clock-in time descending (most recent first).
    /// </summary>
    /// <param name="employeeId">The employee's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A read-only list of <see cref="AttendanceRecord"/> entries.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every currently open attendance session across all employees.
    /// Used to populate the admin dashboard's live view.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A read-only list of all active <see cref="AttendanceRecord"/> entries.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all attendance records whose clock-in date matches the given date.
    /// The date comparison must account for the Europe/Zurich timezone stored in <c>ClockInTime</c>.
    /// </summary>
    /// <param name="date">The calendar date to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A read-only list of <see cref="AttendanceRecord"/> entries for that day.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all attendance records for an employee whose clock-in falls within a half-open
    /// date/time range <c>[from, to)</c>. Both bounds are in Europe/Zurich time.
    /// Used for weekly and monthly hour calculations.
    /// </summary>
    /// <param name="employeeId">The employee's primary key.</param>
    /// <param name="from">Inclusive lower bound (Europe/Zurich time).</param>
    /// <param name="to">Exclusive upper bound (Europe/Zurich time).</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A read-only list of <see cref="AttendanceRecord"/> entries in the range.</returns>
    Task<IReadOnlyList<AttendanceRecord>> GetByDateRangeAsync(
        int employeeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single page of attendance records for an employee and the total record count,
    /// ordered by clock-in time descending. Used for paginated history endpoints.
    /// </summary>
    /// <param name="employeeId">The employee's primary key.</param>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>
    /// A tuple of the page items and the total count across all pages.
    /// </returns>
    Task<(IReadOnlyList<AttendanceRecord> Records, int TotalCount)> GetPagedByEmployeeIdAsync(
        int employeeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new attendance record and returns it with its generated primary key populated.
    /// </summary>
    /// <param name="record">The attendance record to insert.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    /// <returns>The persisted <see cref="AttendanceRecord"/> with <c>Id</c> assigned by the database.</returns>
    Task<AttendanceRecord> AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists mutations on an existing attendance record (e.g., after a clock-out event).
    /// </summary>
    /// <param name="record">The attendance record carrying the updated state.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    Task UpdateAsync(AttendanceRecord record, CancellationToken cancellationToken = default);
}
