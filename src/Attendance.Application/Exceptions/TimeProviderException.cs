namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when the external time provider fails to return a valid timestamp
/// after all configured retry attempts have been exhausted.
/// This is a critical failure — no attendance operation can proceed without a trusted timestamp.
/// </summary>
public sealed class TimeProviderException : Exception
{
    /// <summary>Initializes a new instance with the specified failure message.</summary>
    public TimeProviderException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and the originating cause.</summary>
    public TimeProviderException(string message, Exception innerException)
        : base(message, innerException) { }
}
