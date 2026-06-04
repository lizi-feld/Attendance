namespace Attendance.Application.DTOs;

/// <summary>
/// Extended employee profile that includes the full attendance history.
/// Used in detail/admin endpoints where the complete picture is required.
/// </summary>
public sealed record EmployeeDetailsDto
{
    /// <summary>Gets the employee's unique database identifier.</summary>
    public int Id { get; init; }

    /// <summary>Gets the employee's unique username.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the employee's display name.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets the employee's role as a human-readable string (e.g., <c>"Admin"</c>, <c>"Employee"</c>).</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the employee's complete attendance history,
    /// ordered by clock-in time descending (most recent first).
    /// </summary>
    public IReadOnlyList<AttendanceRecordDto> AttendanceRecords { get; init; } = [];
}
