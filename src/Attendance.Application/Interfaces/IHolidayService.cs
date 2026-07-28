using Attendance.Application.DTOs;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the business operation for retrieving holiday/Parashat data, implementing
/// a cache-aside strategy against the Hebcal API.
/// </summary>
public interface IHolidayService
{
    /// <summary>
    /// Returns all holiday, Chol HaMoed, and Parashat HaShavua entries for the given year.
    /// Fetches from the Hebcal API and persists to the database only if the year is not
    /// already cached; otherwise serves directly from the database.
    /// </summary>
    /// <param name="year">The civil (Gregorian) year to retrieve.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="HolidayDto"/> entries, ordered by date.</returns>
    Task<IReadOnlyList<HolidayDto>> GetHolidaysForYearAsync(int year, CancellationToken cancellationToken = default);
}
