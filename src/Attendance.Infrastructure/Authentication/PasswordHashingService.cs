using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Attendance.Infrastructure.Authentication;

/// <summary>
/// Implements <see cref="IPasswordHashingService"/> using ASP.NET Core Identity's
/// <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA512, 100 000 iterations, 128-bit salt).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PasswordHasher{TUser}"/> does not use the <c>TUser</c> instance during hashing —
/// <c>null!</c> is passed safely as the user argument throughout this implementation.
/// </para>
/// <para>
/// Passwords are <b>never</b> stored or logged in plaintext.
/// All hash comparisons are timing-safe (constant-time) to prevent timing attacks.
/// </para>
/// </remarks>
public sealed class PasswordHashingService : IPasswordHashingService
{
    private readonly IPasswordHasher<Employee> _hasher;

    /// <summary>
    /// Initializes a new instance of <see cref="PasswordHashingService"/>
    /// with the injected Identity password hasher.
    /// </summary>
    /// <param name="hasher">The ASP.NET Core Identity password hasher for <see cref="Employee"/>.</param>
    public PasswordHashingService(IPasswordHasher<Employee> hasher)
    {
        _hasher = hasher;
    }

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));

        // The Employee instance is not used by PasswordHasher<T> internally.
        return _hasher.HashPassword(null!, password);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns <c>true</c> for both <see cref="PasswordVerificationResult.Success"/> and
    /// <see cref="PasswordVerificationResult.SuccessRehashNeeded"/>. The caller may choose to
    /// re-hash and store the updated hash when <c>SuccessRehashNeeded</c> is detected
    /// (indicates an older algorithm iteration count was used).
    /// </remarks>
    public bool VerifyPassword(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password, nameof(password));
        ArgumentException.ThrowIfNullOrWhiteSpace(hash, nameof(hash));

        var result = _hasher.VerifyHashedPassword(null!, hash, password);

        return result is PasswordVerificationResult.Success
                      or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
