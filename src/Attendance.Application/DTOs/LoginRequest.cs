namespace Attendance.Application.DTOs;

/// <summary>
/// HTTP request body payload for the employee authentication endpoint.
/// </summary>
public sealed record LoginRequest
{
    /// <summary>Gets the employee's unique username.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the employee's plaintext password (transmitted over HTTPS, never stored).</summary>
    public string Password { get; init; } = string.Empty;
}
