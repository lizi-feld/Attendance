namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when an operation attempts to assign a username that is already in use.
/// </summary>
public sealed class UsernameAlreadyExistsException : Exception
{
    /// <summary>Gets the duplicate username that triggered the exception.</summary>
    public string Username { get; }

    /// <summary>
    /// Initializes a new instance for the specified username.
    /// </summary>
    /// <param name="username">The username that is already taken.</param>
    public UsernameAlreadyExistsException(string username)
        : base($"Username '{username}' is already taken.")
    {
        Username = username;
    }
}
