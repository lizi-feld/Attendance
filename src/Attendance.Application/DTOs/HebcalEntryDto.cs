namespace Attendance.Application.DTOs;

/// <summary>
/// A single relevant entry parsed from the Hebcal API response
/// (already filtered to "holiday"/"cholhamoed"/"parashat" categories).
/// </summary>
public sealed record HebcalEntryDto
{
    /// <summary>Gets the calendar date this entry applies to.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Gets the Hebrew display name.</summary>
    public string HebrewName { get; init; } = string.Empty;

    /// <summary>Gets the category: "holiday", "cholhamoed", or "parashat".</summary>
    public string Category { get; init; } = string.Empty;
}
