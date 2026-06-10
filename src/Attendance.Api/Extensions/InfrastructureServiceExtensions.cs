using Attendance.Api.HealthChecks;
using Attendance.Api.Infrastructure;
using Attendance.Application.Interfaces;
using Attendance.Infrastructure.ExternalProviders;
using Attendance.Infrastructure.Persistence;
using Attendance.Infrastructure.Repositories;
using Attendance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace Attendance.Api.Extensions;

/// <summary>
/// Extension methods that wire up the Infrastructure layer:
/// SQL Server, repositories, business services, the external time provider,
/// database seeder, and health checks.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure-layer services with the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">Application configuration (connection strings, external API settings).</param>
    /// <param name="environment">Hosting environment used to enable development-only EF Core diagnostics.</param>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        RegisterDatabase(services, configuration, environment);
        RegisterRepositories(services);
        RegisterBusinessServices(services);
        RegisterTimeProvider(services, configuration);
        RegisterHealthChecks(services);

        services.AddTransient<DatabaseSeeder>();

        return services;
    }

    // ── SQL Server ────────────────────────────────────────────────────────────

    private static void RegisterDatabase(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Required connection string 'DefaultConnection' was not found in configuration.");

        services.AddDbContext<AttendanceDbContext>((_, options) =>
        {
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sql.CommandTimeout(30);
                sql.MigrationsAssembly(typeof(AttendanceDbContext).Assembly.FullName);
            });

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            }
        });
    }

    // ── Repositories ─────────────────────────────────────────────────────────

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }

    // ── Business services ─────────────────────────────────────────────────────

    private static void RegisterBusinessServices(IServiceCollection services)
    {
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
    }

    // ── External Time Provider ────────────────────────────────────────────────

    private static void RegisterTimeProvider(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TimeProviderOptions>(
            configuration.GetSection(TimeProviderOptions.SectionName));

        // Register ExternalTimeProvider as a typed HttpClient.
        // HttpClient.Timeout is set to infinite so the Polly resilience pipeline
        // (AttemptTimeout + TotalRequestTimeout inside AddStandardResilienceHandler)
        // controls the timeout budget exclusively.
        //
        // Note: ExternalTimeProvider also contains an internal Polly pipeline (3 retries)
        // as a defence-in-depth measure. The HttpClient-level handler adds 2 more retries
        // at the transport layer before the service-level retry is invoked.
        services.AddHttpClient<ExternalTimeProvider>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TimeProviderOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.MaxRetryAttempts = 2;
            resilience.Retry.BackoffType = DelayBackoffType.Exponential;
            resilience.Retry.UseJitter = true;
            resilience.Retry.Delay = TimeSpan.FromMilliseconds(500);
            resilience.Retry.ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<Polly.Timeout.TimeoutRejectedException>()
                .HandleResult(r =>
                    (int)r.StatusCode >= 500 ||
                    r.StatusCode == System.Net.HttpStatusCode.RequestTimeout);

            resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(12);
            resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(45);
        });

        // Resolve ITimeProvider by forwarding to the typed-client-managed ExternalTimeProvider.
        services.AddTransient<ITimeProvider>(
            sp => sp.GetRequiredService<ExternalTimeProvider>());
    }

    // ── Health Checks ─────────────────────────────────────────────────────────

    private static void RegisterHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>(
                name: "sql-server",
                tags: ["database", "ready"])
            .AddCheck<ExternalTimeProviderHealthCheck>(
                name: "external-time-provider",
                tags: ["external", "ready"]);
    }
}
