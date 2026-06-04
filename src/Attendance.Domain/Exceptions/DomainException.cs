namespace Attendance.Domain.Exceptions;

/// <summary>
/// Represents a domain rule violation within the Time Attendance System.
/// Throw this when an operation violates a business invariant.
/// </summary>
public sealed class DomainException : Exception
{
    /// <summary>Initializes a new instance with the specified violation message.</summary>
    /// <param name="message">A human-readable description of the violated rule.</param>
    public DomainException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and an inner cause.</summary>
    /// <param name="message">A human-readable description of the violated rule.</param>
    /// <param name="innerException">The exception that triggered this violation.</param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
