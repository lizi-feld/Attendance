using Attendance.Application.Interfaces;
using Attendance.Infrastructure.ExternalProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Exposes a lightweight endpoint that returns the current time from the configured external time provider.
/// </summary>
[ApiController]
[Route("api/time")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class TimeController : ControllerBase
{
    private readonly ITimeProvider _timeProvider;
    private readonly IOptions<TimeProviderOptions> _options;
    private readonly ILogger<TimeController> _logger;
    /// <summary>
    ///  Initializes a new instance of <see cref="TimeController"/>.
    /// </summary>
    /// <param name="timeProvider"></param>
    /// <param name="options"></param>
    /// <param name="logger"></param>
    public TimeController(
        ITimeProvider timeProvider,
        IOptions<TimeProviderOptions> options,
        ILogger<TimeController> logger)
    {
        _timeProvider = timeProvider;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current date and time as provided by the external time provider.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("current")]
    [SwaggerOperation(Summary = "Get current time", Description = "Retrieves the current Europe/Zurich time from the configured external provider.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Current time retrieved.")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "External time provider unavailable.")]
    public async Task<IActionResult> GetCurrentTime(CancellationToken cancellationToken)
    {
        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var opts = _options.Value;

        _logger.LogInformation("GET /api/time/current returned time {Time} (TimeZone={TimeZone}) via {BaseUrl}", now, opts.TimeZone, opts.BaseUrl);

        return Ok(new
        {
            CurrentTime = now,
            TimeZone = opts.TimeZone,
            Source = opts.BaseUrl
        });
    }
}
