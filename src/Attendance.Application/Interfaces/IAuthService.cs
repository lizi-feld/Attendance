using Attendance.Application.DTOs;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines authentication and password-security operations for the system.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates the supplied credentials and, on success, issues a signed JWT access token
    /// and a cryptographically random refresh token.
    /// </summary>
    /// <param name="request">The login credentials (username and plaintext password).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="LoginResponse"/> containing the Bearer token, refresh token, expiry, and employee profile.
    /// </returns>
    /// <exception cref="Exceptions.InvalidCredentialsException">
    /// Thrown when the username does not exist or the password is incorrect.
    /// A generic message is used intentionally to prevent username enumeration.
    /// </exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a valid (possibly expired) access token and a non-revoked refresh token
    /// for a new access token and rotated refresh token.
    /// The old refresh token is revoked immediately (single-use enforcement).
    /// </summary>
    /// <param name="request">The access token and refresh token pair to exchange.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="RefreshTokenResponseDto"/> containing the new access token and rotated refresh token.
    /// </returns>
    /// <exception cref="Exceptions.AuthenticationException">
    /// Thrown when the access token is structurally invalid, the refresh token is not found,
    /// expired, revoked, or the tokens do not belong to the same employee.
    /// </exception>
    Task<RefreshTokenResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific refresh token, preventing it from being used for future token exchanges.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token string to revoke.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="Exceptions.AuthenticationException">
    /// Thrown when the refresh token is not found in the system.
    /// </exception>
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes a secure bcrypt hash of the given plaintext password.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>A bcrypt hash string suitable for database storage.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plaintext password matches its stored bcrypt hash.
    /// </summary>
    /// <param name="password">The plaintext password to verify.</param>
    /// <param name="hash">The stored bcrypt hash to compare against.</param>
    /// <returns><c>true</c> if the password is correct; otherwise, <c>false</c>.</returns>
    bool VerifyPassword(string password, string hash);
}
