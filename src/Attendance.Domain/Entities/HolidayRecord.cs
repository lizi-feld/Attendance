namespace Attendance.Domain.Entities;

/// <summary>
/// A single cached calendar entry (holiday, Chol HaMoed day, or Parashat HaShavua)
/// sourced from the Hebcal API and persisted for fast, offline-safe lookups.
/// </summary>
public sealed class HolidayRecord
{
    /// <summary>Required by Entity Framework Core — do not use directly.</summary>
    private HolidayRecord() { }

    /// <summary>Gets the record's unique database identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the calendar date this entry applies to.</summary>
    public DateOnly Date { get; private set; }

    /// <summary>Gets the Hebrew display name (e.g. "א׳ בטבת", "פרשת בראשית").</summary>
    public string HebrewName { get; private set; } = string.Empty;

    /// <summary>Gets the Hebcal category: "holiday", "cholhamoed", or "parashat".</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Factory method that creates and validates a new <see cref="HolidayRecord"/> instance.
    /// </summary>
    public static HolidayRecord Create(DateOnly date, string hebrewName, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hebrewName, nameof(hebrewName));
        ArgumentException.ThrowIfNullOrWhiteSpace(category, nameof(category));

        return new HolidayRecord
        {
            Date = date,
            HebrewName = hebrewName.Trim(),
            Category = category.Trim()
        };
    }
}
