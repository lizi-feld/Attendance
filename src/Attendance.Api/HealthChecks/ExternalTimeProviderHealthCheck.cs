using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Attendance.Api.HealthChecks;

/// <summary>
/// Verifies that the external time provider (TimeAPI.io) is reachable and returning valid data.
/// Reuses the registered <see cref="ITimeProvider"/> (backed by the typed <c>ExternalTimeProvider</c>
/// HttpClient), so no duplicate HttpClient configuration is required.
/// </summary>
public sealed class ExternalTimeProviderHealthCheck : IHealthCheck
{
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<ExternalTimeProviderHealthCheck> _logger;

    /// <summary>Initializes a new instance of <see cref="ExternalTimeProviderHealthCheck"/>.</summary>
    public ExternalTimeProviderHealthCheck(ITimeProvider timeProvider, ILogger<ExternalTimeProviderHealthCheck> logger)
    {
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTime = await _timeProvider.GetCurrentTimeAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                $"External time provider is reachable. Current time: {currentTime:yyyy-MM-dd HH:mm:ss} (Europe/Zurich)");
        }
        catch (TimeProviderException ex)
        {
            _logger.LogWarning(ex, "External time provider health check failed after all retries.");
            return HealthCheckResult.Unhealthy(
                "External time provider is unreachable after all retry attempts.", ex);
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("External time provider health check timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "External time provider health check encountered an unexpected error.");
            return HealthCheckResult.Degraded("External time provider returned an unexpected response.", ex);
        }
    }
}
