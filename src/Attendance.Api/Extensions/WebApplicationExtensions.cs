using Attendance.Api.Infrastructure;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Extensions;

/// <summary>
/// Extension methods on <see cref="WebApplication"/> for database lifecycle management.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Applies any pending EF Core migrations and runs <see cref="DatabaseSeeder"/> in sequence.
    /// Fails fast on any error so the process does not start with a stale or missing schema.
    /// </summary>
    /// <param name="app">The built web application.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task MigrateAndSeedAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var logger   = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var db = services.GetRequiredService<AttendanceDbContext>();

            var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pending.ToList();

            if (migrations.Count != 0)
            {
                logger.LogInformation(
                    "Applying {Count} pending database migration(s): {Migrations}",
                    migrations.Count,
                    string.Join(", ", migrations));

                await db.Database.MigrateAsync(cancellationToken);

                logger.LogInformation("Database migrations applied successfully.");
            }
            else
            {
                logger.LogDebug("No pending database migrations.");
            }

            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(
                ex,
                "Failed to apply database migrations or seed data. " +
                "The application cannot start in a consistent state.");
            throw;
        }
    }
}
