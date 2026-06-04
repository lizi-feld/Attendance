using Attendance.Domain.Enums;

namespace Attendance.Application.Commands;

/// <summary>
/// Command to register a new employee account in the system.
/// Validated by <c>CreateEmployeeCommandValidator</c> before reaching the service layer.
/// </summary>
public sealed record CreateEmployeeCommand
{
    /// <summary>Gets the unique username for the new account (max 100 chars).</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets the plaintext password. The service layer will hash this before storage;
    /// it must never be persisted or logged in plaintext.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Gets the employee's display name (max 200 chars).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets the access role to assign to the new employee.</summary>
    public Role Role { get; init; }
}
