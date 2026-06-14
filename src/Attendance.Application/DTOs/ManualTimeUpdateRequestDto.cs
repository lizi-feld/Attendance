namespace Attendance.Application.DTOs;

/// <summary>
/// Request payload for a manual/retroactive attendance record update.
/// The <see cref="Note"/> is strictly required for this operation to satisfy the
/// audit trail business rule — it is NOT required for regular clock-in/out.
/// </summary>
public sealed record ManualTimeUpdateRequestDto
{
    /// <summary>Gets the primary key of the attendance record to modify.</summary>
    public int RecordId { get; init; }

    /// <summary>Gets the replacement clock-in timestamp.</summary>
    public DateTime NewClockInTime { get; init; }

    /// <summary>Gets the replacement clock-out timestamp; must be after <see cref="NewClockInTime"/>.</summary>
    public DateTime NewClockOutTime { get; init; }

    /// <summary>Gets the mandatory reason for the retroactive change.</summary>
    public string? Note { get; init; }
}
