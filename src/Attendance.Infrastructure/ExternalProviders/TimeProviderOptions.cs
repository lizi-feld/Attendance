namespace Attendance.Infrastructure.ExternalProviders;

/// <summary>
/// Strongly-typed configuration for the external time provider.
/// Bound from the <c>"TimeProvider"</c> section of <c>appsettings.json</c>.
/// </summary>
/// <example>
/// <code>
/// "TimeProvider": {
///   "BaseUrl": "https://timeapi.io/",
///   "TimeZone": "Europe/Zurich",
///   "MaxRetryAttempts": 3,
///   "RetryDelaySeconds": 1,
///   "TimeoutSeconds": 10
/// }
/// </code>
/// </example>
public sealed class TimeProviderOptions
{
    /// <summary>The configuration section key used when binding from appsettings.</summary>
    public const string SectionName = "TimeProvider";

    /// <summary>Gets the base URL of the TimeAPI.io service (include trailing slash).</summary>
    public string BaseUrl { get; init; } = "https://timeapi.io/";

    /// <summary>Gets the IANA timezone identifier to request (must match TimeAPI.io's accepted values).</summary>
    public string TimeZone { get; init; } = "Europe/Zurich";

    /// <summary>Gets the maximum number of retry attempts on transient HTTP failure.</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Gets the base delay in seconds between retry attempts (exponential backoff applied).</summary>
    public int RetryDelaySeconds { get; init; } = 1;

    /// <summary>Gets the per-request HTTP timeout in seconds.</summary>
    public int TimeoutSeconds { get; init; } = 10;
}
