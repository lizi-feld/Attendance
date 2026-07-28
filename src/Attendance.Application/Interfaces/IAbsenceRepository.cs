using Attendance.Domain.Entities;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the data access contract for <see cref="AbsenceRecord"/> operations.
/// Implementations live in the Infrastructure layer and are injected via DI.
/// </summary>
public interface IAbsenceRepository
{
    /// <summary>
    /// Persists a new absence record and returns it with its generated primary key populated.
    /// </summary>
    /// <param name="record">The absence record to insert.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    /// <returns>The persisted <see cref="AbsenceRecord"/> with <c>Id</c> assigned by the database.</returns>
    Task<AbsenceRecord> AddAsync(AbsenceRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the absence record whose <see cref="AbsenceRecord.DocumentUrl"/> matches exactly.
    /// Used to authorize document downloads — only the owning employee (or an admin) may fetch it.
    /// </summary>
    /// <param name="documentUrl">The exact document URL to search for.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>The matching <see cref="AbsenceRecord"/>, or <c>null</c> if no record references it.</returns>
    Task<AbsenceRecord?> GetByDocumentUrlAsync(string documentUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all absence records for an employee whose date falls within a half-open
    /// range <c>[from, to)</c>. Used to merge absence data into the month calendar view.
    /// </summary>
    /// <param name="employeeId">The employee's primary key.</param>
    /// <param name="from">Inclusive lower bound.</param>
    /// <param name="to">Exclusive upper bound.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A read-only list of <see cref="AbsenceRecord"/> entries in the range.</returns>
    Task<IReadOnlyList<AbsenceRecord>> GetByEmployeeIdAndDateRangeAsync(
        int employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);
}
