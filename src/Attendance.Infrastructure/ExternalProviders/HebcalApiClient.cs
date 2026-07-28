using System.Text.Json;
using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Infrastructure.ExternalProviders.Models;
using Microsoft.Extensions.Logging;

namespace Attendance.Infrastructure.ExternalProviders;

/// <summary>
/// Fetches Jewish holidays, Chol HaMoed days, and Parashat HaShavua from the Hebcal API.
/// Registered as a typed <see cref="HttpClient"/> — the base address is configured during DI setup.
/// Only "holiday", "cholhamoed", and "parashat" categories are returned; candle-lighting/havdalah
/// entries and anything else are filtered out here so downstream code never sees them.
/// </summary>
public sealed class HebcalApiClient : IHebcalClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> RelevantCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "holiday", "cholhamoed", "parashat"
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HebcalApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="HebcalApiClient"/>.
    /// </summary>
    /// <param name="httpClient">Typed HTTP client with <see cref="HebcalOptions.BaseUrl"/> as base address.</param>
    /// <param name="logger">Structured logger for failure events.</param>
    public HebcalApiClient(HttpClient httpClient, ILogger<HebcalApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="HebcalApiException">
    /// Thrown when the HTTP call fails or the response body is missing or unparsable.
    /// </exception>
    public async Task<IReadOnlyList<HebcalEntryDto>> GetYearEventsAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"hebcal?v=1&cfg=json&year={year}&i=on&maj=on&min=on&nx=on&mod=on&s=on";

        try
        {
            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HebcalApiException(
                    $"Hebcal API responded with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) for year {year}.");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<HebcalResponse>(json, JsonOptions);

            if (parsed is null)
            {
                throw new HebcalApiException(
                    $"Hebcal API returned an empty or invalid response body for year {year}.");
            }

            return parsed.Items
                .Where(i => RelevantCategories.Contains(i.Category) && !string.IsNullOrWhiteSpace(i.Hebrew))
                .Select(i => new HebcalEntryDto
                {
                    // Candle/havdalah entries carry a time component; holiday/cholhamoed/parashat
                    // entries don't, but the first 10 chars are taken defensively either way.
                    Date = DateOnly.Parse(i.Date.Length >= 10 ? i.Date[..10] : i.Date),
                    HebrewName = i.Hebrew,
                    Category = i.Category.ToLowerInvariant()
                })
                .ToList();
        }
        catch (HebcalApiException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error fetching holidays from Hebcal API for year {Year}.", year);
            throw new HebcalApiException(
                $"An unexpected error occurred while retrieving holidays for year {year}.", ex);
        }
    }
}
