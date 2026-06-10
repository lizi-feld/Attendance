namespace Attendance.Application.DTOs;

/// <summary>
/// HTTP request body for the admin-only <c>PUT /api/auth/update/{id}</c> endpoint.
/// All fields are optional; only non-null values are applied.
/// </summary>
public sealed record UpdateEmployeeRequest
{
    /// <summary>Gets the new display name, or <c>null</c> to leave unchanged.</summary>
    public string? FullName { get; init; }

    /// <summary>Gets the new login username, or <c>null</c> to leave unchanged.</summary>
    public string? Username { get; init; }

    /// <summary>Gets the new plaintext password, or <c>null</c> to leave unchanged.</summary>
    public string? Password { get; init; }
}
