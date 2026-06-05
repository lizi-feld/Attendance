namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when login credentials are invalid.
/// A generic, non-specific message is used intentionally to prevent username enumeration attacks —
/// callers must not leak whether the username or the password was wrong.
/// </summary>
public sealed class InvalidCredentialsException : Exception
{
    private const string DefaultMessage = "The provided credentials are invalid.";

    /// <summary>Initializes a new instance with the standard security-neutral message.</summary>
    public InvalidCredentialsException() : base(DefaultMessage) { }

    /// <summary>Initializes a new instance with a custom message (use sparingly — avoid detail that aids enumeration).</summary>
    public InvalidCredentialsException(string message) : base(message) { }
}
