using System.ComponentModel.DataAnnotations;
using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Serves Jewish holidays, Chol HaMoed days, and Parashat HaShavua data.
/// The database is the single source of truth — the Hebcal external API is only
/// called once per year, the first time that year is requested (cache-aside).
/// </summary>
[ApiController]
[Route("api/holidays")]
[Authorize(Roles = "Employee,Admin")]
[Produces("application/json")]
public sealed class HolidaysController : ControllerBase
{
    private readonly IHolidayService _holidayService;
    private readonly ILogger<HolidaysController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="HolidaysController"/>.
    /// </summary>
    public HolidaysController(IHolidayService holidayService, ILogger<HolidaysController> logger)
    {
        _holidayService = holidayService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all holidays, Chol HaMoed days, and Parashat HaShavua entries for the given year.
    /// </summary>
    /// <param name="year">The civil (Gregorian) year to retrieve.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>A list of calendar entries for the year, ordered by date.</returns>
    [HttpGet("{year:int}")]
    [SwaggerOperation(
        Summary = "Get holidays for year",
        Description = "Returns Jewish holidays, Chol HaMoed days, and Parashat HaShavua for the given year. " +
                      "Cached in the database after the first request per year.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Holidays retrieved successfully.", typeof(IReadOnlyList<HolidayDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(IReadOnlyList<HolidayDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),            StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByYear(
        [FromRoute][Range(1900, 2200)] int year,
        CancellationToken cancellationToken)
    {
        var result = await _holidayService.GetHolidaysForYearAsync(year, cancellationToken);

        _logger.LogInformation("GET /api/holidays/{Year} succeeded. Count={Count}", year, result.Count);

        return Ok(result);
    }
}
