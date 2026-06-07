using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Domain.Enums;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Api.Infrastructure;

/// <summary>
/// Seeds essential baseline data into the database during application startup.
/// All seed operations are idempotent — safe to call on every startup.
/// Must be invoked after migrations have been applied.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly AttendanceDbContext _context;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>Initializes a new instance of <see cref="DatabaseSeeder"/>.</summary>
    public DatabaseSeeder(
        AttendanceDbContext context,
        IPasswordHashingService passwordHashingService,
        ITimeProvider timeProvider,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHashingService = passwordHashingService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Executes all seed operations in order.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the seeding operation.</param>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        const string adminUsername = "admin";

        var exists = await _context.Employees
            .AnyAsync(e => e.Username == adminUsername, cancellationToken);

        if (exists)
        {
            _logger.LogDebug("Admin user already exists. Skipping seed.");
            return;
        }

        // Attempt to use the external time provider for the audit timestamp.
        // Fall back to UTC if the provider is unavailable during initial startup.
        DateTime now;
        try
        {
            now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        }
        catch (TimeProviderException ex)
        {
            _logger.LogWarning(ex,
                "External time provider unavailable during database seed. " +
                "Using UTC as fallback for the admin account CreatedAt timestamp.");
            now = DateTime.UtcNow;
        }

        var passwordHash = _passwordHashingService.HashPassword("Admin123!");

        var admin = Employee.Create(
            username: adminUsername,
            passwordHash: passwordHash,
            fullName: "System Administrator",
            role: Role.Admin,
            createdAt: now);

        await _context.Employees.AddAsync(admin, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Admin user seeded. Username={Username} FullName={FullName} Role={Role} CreatedAt={CreatedAt}",
            admin.Username, admin.FullName, admin.Role, admin.CreatedAt);
    }
}
