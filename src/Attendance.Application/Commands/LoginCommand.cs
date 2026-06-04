namespace Attendance.Application.Commands;

/// <summary>
/// Command to authenticate an employee and issue a JWT Bearer token.
/// Constructed from <c>LoginRequest</c> in the controller before reaching the service layer.
/// </summary>
public sealed record LoginCommand
{
    /// <summary>Gets the employee's username credential.</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the employee's plaintext password credential (never logged or stored).</summary>
    public string Password { get; init; } = string.Empty;
}
