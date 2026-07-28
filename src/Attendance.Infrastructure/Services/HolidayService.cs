using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IHolidayService"/> using a cache-aside strategy: the database is the
/// single source of truth for the API; the Hebcal external API is only ever called once per
/// year, the first time that year is requested.
/// </summary>
public sealed class HolidayService : IHolidayService
{
    private readonly IHolidayRepository _holidayRepository;
    private readonly IHebcalClient _hebcalClient;
    private readonly ILogger<HolidayService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="HolidayService"/> with all required dependencies.
    /// </summary>
    public HolidayService(
        IHolidayRepository holidayRepository,
        IHebcalClient hebcalClient,
        ILogger<HolidayService> logger)
    {
        _holidayRepository = holidayRepository;
        _hebcalClient = hebcalClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysForYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var alreadyCached = await _holidayRepository.ExistsForYearAsync(year, cancellationToken);

        if (!alreadyCached)
        {
            _logger.LogInformation("No cached holidays for {Year}. Fetching from Hebcal API.", year);

            var entries = await _hebcalClient.GetYearEventsAsync(year, cancellationToken);
            var records = entries
                .Select(e => HolidayRecord.Create(e.Date, e.HebrewName, e.Category))
                .ToList();

            if (records.Count > 0)
                await _holidayRepository.AddRangeAsync(records, cancellationToken);

            _logger.LogInformation("Cached {Count} holiday entries for {Year}.", records.Count, year);
        }

        var stored = await _holidayRepository.GetByYearAsync(year, cancellationToken);
        return stored.Select(MapToDto).ToList();
    }

    private static HolidayDto MapToDto(HolidayRecord record) => new()
    {
        Date = record.Date,
        HebrewName = record.HebrewName,
        Category = record.Category
    };
}
