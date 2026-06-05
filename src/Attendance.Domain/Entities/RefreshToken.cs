using Attendance.Domain.Exceptions;

namespace Attendance.Domain.Entities;

/// <summary>
/// Represents a single-use, cryptographically random refresh token issued to an employee
/// upon successful authentication. Refresh tokens rotate on every use.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>Required by Entity Framework Core — do not use directly.</summary>
    private RefreshToken() { }

    /// <summary>Gets the refresh token's unique database identifier.</summary>
    public int Id { get; private set; }

    /// <summary>
    /// Gets the cryptographically random, base64-encoded token string.
    /// This value is transmitted to the client and stored as a hash-equivalent (64 bytes → 88 chars).
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>Gets the foreign key referencing the owning <see cref="Employee"/>.</summary>
    public int EmployeeId { get; private set; }

    /// <summary>
    /// Gets the Europe/Zurich timestamp at which this token expires.
    /// Sourced from <c>ITimeProvider</c> at creation time.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Gets the Europe/Zurich timestamp when this token was issued.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the Europe/Zurich timestamp when this token was revoked,
    /// or <c>null</c> if it has not been revoked.
    /// </summary>
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Gets the navigation property to the owning <see cref="Employee"/>.</summary>
    public Employee Employee { get; private set; } = null!;

    /// <summary>Gets whether this token has been explicitly revoked.</summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>
    /// Determines whether this token is still usable at the given point in time.
    /// A token is active when it is neither revoked nor past its expiry.
    /// </summary>
    /// <param name="now">The current time from <c>ITimeProvider</c> (Europe/Zurich).</param>
    public bool IsActiveAt(DateTime now) => !IsRevoked && now < ExpiresAt;

    /// <summary>
    /// Factory method that creates a new, unrevoked refresh token.
    /// </summary>
    /// <param name="token">The cryptographically random token string (64-byte base64).</param>
    /// <param name="employeeId">The ID of the employee this token belongs to.</param>
    /// <param name="expiresAt">Expiry timestamp from the external time provider.</param>
    /// <param name="createdAt">Creation timestamp from the external time provider.</param>
    /// <returns>A valid, unsaved <see cref="RefreshToken"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when any string argument is null or whitespace.</exception>
    /// <exception cref="DomainException">Thrown when <paramref name="expiresAt"/> is not after <paramref name="createdAt"/>.</exception>
    public static RefreshToken Create(string token, int employeeId, DateTime expiresAt, DateTime createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));

        if (employeeId <= 0)
            throw new DomainException("Employee ID must be a positive integer.");

        if (expiresAt <= createdAt)
            throw new DomainException("Refresh token expiry must be after its creation time.");

        return new RefreshToken
        {
            Token = token,
            EmployeeId = employeeId,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt
        };
    }

    /// <summary>
    /// Marks this token as revoked, preventing any future use.
    /// </summary>
    /// <param name="revokedAt">Revocation timestamp from the external time provider.</param>
    /// <exception cref="DomainException">Thrown when the token is already revoked.</exception>
    public void Revoke(DateTime revokedAt)
    {
        if (IsRevoked)
            throw new DomainException($"Refresh token {Id} is already revoked.");

        RevokedAt = revokedAt;
    }
}
