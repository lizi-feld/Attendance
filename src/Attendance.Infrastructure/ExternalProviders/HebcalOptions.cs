namespace Attendance.Infrastructure.ExternalProviders;

/// <summary>
/// Strongly-typed configuration for the Hebcal external API.
/// Bound from the <c>"Hebcal"</c> section of <c>appsettings.json</c>.
/// </summary>
public sealed class HebcalOptions
{
    /// <summary>The configuration section key used when binding from appsettings.</summary>
    public const string SectionName = "Hebcal";

    /// <summary>Gets the base URL of the Hebcal service (include trailing slash).</summary>
    public string BaseUrl { get; init; } = "https://www.hebcal.com/";
}
