using Attendance.Domain.Enums;

namespace Attendance.Application.DTOs;

/// <summary>
/// HTTP request body for the admin-only <c>POST /api/auth/add-employee</c> endpoint.
/// </summary>
public sealed record AddEmployeeRequest
{
    /// <summary>Gets the employee's display name.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets the unique login username.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the plaintext password (transmitted over HTTPS, never stored).</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Gets the access role to assign. Defaults to <see cref="Role.Employee"/>.</summary>
    public Role Role { get; init; } = Role.Employee;
}
