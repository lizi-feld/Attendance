namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines secure password hashing and verification operations.
/// Implemented in the Infrastructure layer using ASP.NET Core Identity's
/// <c>PasswordHasher&lt;TUser&gt;</c> (PBKDF2 with HMAC-SHA512, 100 000 iterations).
/// </summary>
public interface IPasswordHashingService
{
    /// <summary>
    /// Produces a secure hash of the given plaintext password suitable for database storage.
    /// </summary>
    /// <param name="password">The plaintext password to hash. Must not be null or whitespace.</param>
    /// <returns>An opaque hash string that encodes the algorithm, salt, and digest.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plaintext password matches a previously computed hash.
    /// </summary>
    /// <param name="password">The plaintext password to test.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns>
    /// <c>true</c> when the password is correct (including cases where re-hashing is recommended);
    /// <c>false</c> otherwise.
    /// </returns>
    bool VerifyPassword(string password, string hash);
}
