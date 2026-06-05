namespace Attendance.Application.DTOs;

/// <summary>
/// Represents a worked-hours summary for a specific period (week or month).
/// All values are derived from attendance records sourced via the external time provider.
/// </summary>
public sealed record WorkedHoursDto
{
    /// <summary>Gets the total hours worked, rounded to two decimal places.</summary>
    public double TotalHours { get; init; }

    /// <summary>Gets the total minutes worked (may exceed 60 for multi-hour periods).</summary>
    public long TotalMinutes { get; init; }

    /// <summary>Gets the duration formatted as <c>HH:MM:SS</c> (hours may exceed 23).</summary>
    public string Formatted { get; init; } = string.Empty;

    /// <summary>
    /// Creates a <see cref="WorkedHoursDto"/> from a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="duration">The total worked duration for the requested period.</param>
    public static WorkedHoursDto FromTimeSpan(TimeSpan duration) => new()
    {
        TotalHours = Math.Round(duration.TotalHours, 2),
        TotalMinutes = (long)duration.TotalMinutes,
        Formatted = $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}"
    };
}
