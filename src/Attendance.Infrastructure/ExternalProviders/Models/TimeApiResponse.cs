using System.Text.Json.Serialization;

namespace Attendance.Infrastructure.ExternalProviders.Models;

/// <summary>
/// Internal deserialization contract for the TimeAPI.io JSON response.
/// Only the fields consumed by <see cref="ExternalTimeProvider"/> are declared.
/// </summary>
internal sealed record TimeApiResponse
{
    /// <summary>
    /// ISO 8601 datetime string in the requested IANA timezone
    /// (e.g., <c>"2024-06-05T10:30:45.1234567"</c>).
    /// This is the only field used for timestamp extraction.
    /// </summary>
    [JsonPropertyName("dateTime")]
    public string DateTime { get; init; } = string.Empty;

    /// <summary>
    /// The IANA timezone identifier echoed back by the API
    /// (e.g., <c>"Europe/Zurich"</c>). Used for defensive validation.
    /// </summary>
    [JsonPropertyName("timeZone")]
    public string TimeZone { get; init; } = string.Empty;
}
