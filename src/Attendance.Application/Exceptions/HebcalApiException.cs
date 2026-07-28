namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when the Hebcal external API call fails or returns an unparsable response.
/// </summary>
public sealed class HebcalApiException : Exception
{
    /// <summary>Initializes a new instance with the specified failure message.</summary>
    public HebcalApiException(string message) : base(message) { }

    /// <summary>Initializes a new instance with a message and an inner cause.</summary>
    public HebcalApiException(string message, Exception innerException)
        : base(message, innerException) { }
}
