using System.Text.Json.Serialization;

namespace Attendance.Infrastructure.ExternalProviders.Models;

/// <summary>
/// Internal deserialization contract for the Hebcal API JSON response.
/// Only the fields consumed by <see cref="HebcalApiClient"/> are declared.
/// </summary>
internal sealed record HebcalResponse
{
    [JsonPropertyName("items")]
    public List<HebcalItem> Items { get; init; } = [];
}

/// <summary>A single raw event entry from the Hebcal API.</summary>
internal sealed record HebcalItem
{
    /// <summary>ISO date, e.g. "2026-01-01" (candle-lighting entries include a time — not requested here).</summary>
    [JsonPropertyName("date")]
    public string Date { get; init; } = string.Empty;

    /// <summary>Hebrew display name, e.g. "א׳ בטבת", "פרשת בראשית".</summary>
    [JsonPropertyName("hebrew")]
    public string Hebrew { get; init; } = string.Empty;

    /// <summary>Event category, e.g. "holiday", "cholhamoed", "parashat", "candles", "havdalah".</summary>
    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;
}
