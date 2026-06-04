using Attendance.Application.DTOs;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines authentication and password-security operations for the system.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates the supplied credentials and, on success, issues a signed JWT token.
    /// </summary>
    /// <param name="request">The login credentials (username and plaintext password).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="LoginResponse"/> containing the Bearer token, expiry, and employee profile.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the username does not exist or the password is incorrect.
    /// </exception>
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

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
