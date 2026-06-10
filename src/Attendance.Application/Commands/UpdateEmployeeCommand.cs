namespace Attendance.Application.Commands;

/// <summary>
/// Command to update an existing employee's mutable fields.
/// Only non-null properties are applied; omitted properties remain unchanged.
/// Validated by <c>UpdateEmployeeCommandValidator</c> before reaching the service layer.
/// </summary>
public sealed record UpdateEmployeeCommand
{
    /// <summary>Gets the unique identifier of the employee to update.</summary>
    public int Id { get; init; }

    /// <summary>Gets the new display name, or <c>null</c> to leave unchanged.</summary>
    public string? FullName { get; init; }

    /// <summary>Gets the new username, or <c>null</c> to leave unchanged.</summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets the new plaintext password, or <c>null</c> to leave unchanged.
    /// The service layer hashes this before storage; it must never be persisted or logged.
    /// </summary>
    public string? Password { get; init; }
}
