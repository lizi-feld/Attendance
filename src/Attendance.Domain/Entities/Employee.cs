using Attendance.Domain.Enums;
using Attendance.Domain.Exceptions;

namespace Attendance.Domain.Entities;

/// <summary>
/// Aggregate root representing an employee in the Time Attendance System.
/// All mutations are performed through domain methods to enforce business invariants.
/// </summary>
public sealed class Employee
{
    private readonly List<AttendanceRecord> _attendanceRecords = [];

    /// <summary>Required by Entity Framework Core — do not use directly.</summary>
    private Employee() { }

    /// <summary>Gets the employee's unique database identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the employee's unique login username (stored lowercase).</summary>
    public string Username { get; private set; } = string.Empty;

    /// <summary>Gets the bcrypt-hashed password. Never expose in API responses.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Gets the employee's display name.</summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>Gets the role assigned to this employee.</summary>
    public Role Role { get; private set; }

    /// <summary>Gets the UTC timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the employee's complete attendance history.
    /// EF Core populates this via the shadow backing field <c>_attendanceRecords</c>.
    /// </summary>
    public IReadOnlyCollection<AttendanceRecord> AttendanceRecords => _attendanceRecords.AsReadOnly();

    /// <summary>
    /// Factory method that creates and validates a new <see cref="Employee"/> instance.
    /// </summary>
    /// <param name="username">Unique username (max 100 chars, stored as lowercase).</param>
    /// <param name="passwordHash">Pre-computed bcrypt hash of the password.</param>
    /// <param name="fullName">Display name (max 200 chars).</param>
    /// <param name="role">The access role to assign.</param>
    /// <param name="createdAt">Timestamp from the external time provider (Europe/Zurich).</param>
    /// <returns>A valid, unsaved <see cref="Employee"/> entity.</returns>
    /// <exception cref="ArgumentException">Thrown when any string argument is null or whitespace.</exception>
    /// <exception cref="DomainException">Thrown when a business constraint is violated.</exception>
    public static Employee Create(
        string username,
        string passwordHash,
        string fullName,
        Role role,
        DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username, nameof(username));
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName, nameof(fullName));

        if (username.Length > 100)
            throw new DomainException("Username cannot exceed 100 characters.");

        if (fullName.Length > 200)
            throw new DomainException("Full name cannot exceed 200 characters.");

        return new Employee
        {
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Replaces the stored password hash with a newly computed one.
    /// </summary>
    /// <param name="newHash">The new bcrypt hash to store.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newHash"/> is null or whitespace.</exception>
    public void UpdatePasswordHash(string newHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newHash, nameof(newHash));
        PasswordHash = newHash;
    }

    /// <summary>
    /// Elevates the employee's role to <see cref="Role.Admin"/>.
    /// </summary>
    /// <exception cref="DomainException">Thrown when the employee is already an administrator.</exception>
    public void PromoteToAdmin()
    {
        if (Role == Role.Admin)
            throw new DomainException($"Employee '{Username}' is already an administrator.");

        Role = Role.Admin;
    }
}
