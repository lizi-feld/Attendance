using Attendance.Domain.Entities;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the data access contract for <see cref="HolidayRecord"/> operations.
/// Implementations live in the Infrastructure layer and are injected via DI.
/// </summary>
public interface IHolidayRepository
{
    /// <summary>Returns whether any holiday records are already cached for the given year.</summary>
    Task<bool> ExistsForYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all cached holiday records for the given year, ordered by date.</summary>
    Task<IReadOnlyList<HolidayRecord>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>Persists a batch of newly fetched holiday records.</summary>
    Task AddRangeAsync(IEnumerable<HolidayRecord> records, CancellationToken cancellationToken = default);
}
