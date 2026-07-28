namespace Attendance.Application.DTOs;

/// <summary>
/// A single calendar entry (holiday, Chol HaMoed day, or Parashat HaShavua) for API consumption.
/// </summary>
public sealed record HolidayDto
{
    /// <summary>Gets the calendar date this entry applies to.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Gets the Hebrew display name.</summary>
    public string HebrewName { get; init; } = string.Empty;

    /// <summary>Gets the category: "holiday", "cholhamoed", or "parashat".</summary>
    public string Category { get; init; } = string.Empty;
}
