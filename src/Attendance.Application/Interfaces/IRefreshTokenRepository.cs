using Attendance.Domain.Entities;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines data access operations for <see cref="RefreshToken"/> persistence.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves a refresh token by its raw token string value.
    /// </summary>
    /// <param name="token">The opaque token string received from the client.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>The matching <see cref="RefreshToken"/> with <c>Employee</c> loaded, or <c>null</c> if not found.</returns>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new refresh token and returns it with its generated primary key.
    /// </summary>
    /// <param name="refreshToken">The refresh token entity to insert.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    /// <returns>The persisted <see cref="RefreshToken"/> with <c>Id</c> assigned by the database.</returns>
    Task<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists mutations on an existing refresh token (e.g., after revocation).
    /// </summary>
    /// <param name="refreshToken">The refresh token carrying updated state (e.g., <c>RevokedAt</c> set).</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all non-revoked refresh tokens for a specific employee in a single operation.
    /// Useful for implementing "sign out everywhere" or security-breach responses.
    /// </summary>
    /// <param name="employeeId">The employee whose active tokens should all be revoked.</param>
    /// <param name="revokedAt">The revocation timestamp from the external time provider.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    Task RevokeAllForEmployeeAsync(int employeeId, DateTime revokedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all refresh tokens whose <c>ExpiresAt</c> is before the given cutoff.
    /// Intended to be called periodically by a background job to keep the table compact.
    /// </summary>
    /// <param name="cutoff">Only tokens with <c>ExpiresAt &lt; cutoff</c> are deleted.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    Task DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}
