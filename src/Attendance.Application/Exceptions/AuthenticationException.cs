namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when an authentication or token operation fails for a reason other than
/// invalid credentials (e.g., an invalid, expired, or revoked refresh token).
/// </summary>
public sealed class AuthenticationException : Exception
{
    /// <summary>Initializes a new instance with the specified failure message.</summary>
    public AuthenticationException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the originating cause.</summary>
    public AuthenticationException(string message, Exception innerException)
        : base(message, innerException) { }
}
