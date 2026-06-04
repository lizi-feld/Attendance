namespace Attendance.Application.DTOs;

/// <summary>
/// Aggregated real-time attendance metrics for the administrator dashboard.
/// All time values reflect Europe/Zurich time as sourced from the external time provider.
/// </summary>
public sealed record DashboardSummaryDto
{
    /// <summary>Gets the total number of registered employees in the system.</summary>
    public int TotalEmployees { get; init; }

    /// <summary>Gets the number of employees who are currently clocked in.</summary>
    public int ClockedInNow { get; init; }

    /// <summary>Gets the total number of attendance sessions recorded today.</summary>
    public int TotalRecordsToday { get; init; }

    /// <summary>Gets the list of all currently open attendance sessions across all employees.</summary>
    public IReadOnlyList<AttendanceRecordDto> ActiveSessions { get; init; } = [];

    /// <summary>
    /// Gets the Europe/Zurich timestamp at which this summary snapshot was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; }
}
