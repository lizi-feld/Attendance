namespace Attendance.Application.DTOs;

/// <summary>
/// Lightweight employee summary used in list endpoints and embedded in JWT responses.
/// Does not include sensitive data or the full attendance history.
/// </summary>
public sealed record EmployeeDto
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
}
