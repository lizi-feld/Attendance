using Attendance.Application.DTOs;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the contract for fetching holiday/Parashat data from the external Hebcal API.
/// Implementations live in the Infrastructure layer and are injected via DI.
/// </summary>
public interface IHebcalClient
{
    /// <summary>
    /// Fetches all holiday, Chol HaMoed, and Parashat HaShavua entries for the given Hebrew/civil year.
    /// </summary>
    /// <param name="year">The civil (Gregorian) year to fetch.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of relevant entries, already filtered by category.</returns>
    /// <exception cref="Exceptions.HebcalApiException">
    /// Thrown when the API call fails or returns an unparsable response.
    /// </exception>
    Task<IReadOnlyList<HebcalEntryDto>> GetYearEventsAsync(int year, CancellationToken cancellationToken = default);
}
