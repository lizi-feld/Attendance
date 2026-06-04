namespace Attendance.Application.Queries;

/// <summary>
/// Query to generate a real-time attendance dashboard snapshot.
/// Restricted to administrators. The "today" boundary is determined by the external time provider.
/// </summary>
public sealed record GetDashboardSummaryQuery;
