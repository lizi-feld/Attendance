namespace Attendance.Application.DTOs;

/// <summary>
/// Request payload for manually creating a new historical attendance record.
/// Both <see cref="ClockInTime"/> and <see cref="ClockOutTime"/> are combined with
/// <see cref="Date"/> in the service layer to produce full <see cref="DateTime"/> values.
/// <para>
/// The <see cref="Note"/> is strictly required for the audit trail — identical to the
/// manual-update business rule.
/// </para>
/// <para>
/// <see cref="EmployeeId"/> is optional and intended for admin use only.
/// When omitted on the admin endpoint the acting admin's own ID is used as the target.
/// The field is ignored entirely on the employee endpoint.
/// </para>
/// </summary>
public sealed record ManualAddShiftRequestDto
{
    /// <summary>Gets the calendar date of the shift being added.</summary>
    public DateOnly Date { get; init; }

    /// <summary>Gets the clock-in time of day for the shift.</summary>
    public TimeOnly ClockInTime { get; init; }

    /// <summary>Gets the clock-out time of day for the shift; must be after <see cref="ClockInTime"/>.</summary>
    public TimeOnly ClockOutTime { get; init; }

    /// <summary>Gets the mandatory reason explaining why this record is being created manually.</summary>
    public string? Note { get; init; }

    /// <summary>
    /// Gets the target employee ID.
    /// Admin endpoint: if provided, the shift is created for this employee; otherwise the admin's own ID is used.
    /// Employee endpoint: ignored — the authenticated user's ID is always used.
    /// </summary>
    public int? EmployeeId { get; init; }
}
