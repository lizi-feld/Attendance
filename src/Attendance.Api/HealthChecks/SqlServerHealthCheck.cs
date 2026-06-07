using Attendance.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Attendance.Api.HealthChecks;

/// <summary>
/// Verifies that the application can open a connection to SQL Server via
/// the configured <see cref="AttendanceDbContext"/>.
/// Creates a fresh DI scope per check to correctly handle the scoped DbContext lifetime.
/// </summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqlServerHealthCheck> _logger;

    /// <summary>Initializes a new instance of <see cref="SqlServerHealthCheck"/>.</summary>
    public SqlServerHealthCheck(IServiceScopeFactory scopeFactory, ILogger<SqlServerHealthCheck> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AttendanceDbContext>();

            var canConnect = await db.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server connection test returned false.");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("SQL Server health check timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQL Server health check failed.");
            return HealthCheckResult.Unhealthy("SQL Server is unreachable.", ex);
        }
    }
}
