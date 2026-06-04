namespace Attendance.Application.Interfaces;

/// <summary>
/// Provides the current time from an authoritative external source.
/// <para>
/// <b>Contract:</b> All attendance timestamps must originate exclusively from this provider.
/// The canonical timezone for all returned values is <c>Europe/Zurich</c> (CET / CEST).
/// Using <see cref="DateTime.Now"/> or <see cref="DateTime.UtcNow"/> anywhere in the
/// attendance flow is a violation of this requirement.
/// </para>
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Asynchronously retrieves the current date and time from the configured external provider.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the network request.</param>
    /// <returns>
    /// The current <see cref="DateTime"/> in the <c>Europe/Zurich</c> timezone,
    /// with <see cref="DateTimeKind.Unspecified"/> kind (timezone is implicit from the provider contract).
    /// </returns>
    Task<DateTime> GetCurrentTimeAsync(CancellationToken cancellationToken = default);
}
