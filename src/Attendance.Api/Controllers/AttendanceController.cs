using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Provides clock-in/out and attendance history endpoints for the authenticated employee.
/// UserId is extracted from the JWT — employees can only access their own records.
/// </summary>
[ApiController]
[Route("api/attendance")]
[Authorize(Roles = "Employee,Admin")]
[Produces("application/json")]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceController"/>.
    /// </summary>
    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Records a clock-in event for the authenticated employee.
    /// The timestamp is sourced from the external time provider (Europe/Zurich).
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly created attendance record.</returns>
    [HttpPost("clock-in")]
    [SwaggerOperation(
        Summary = "Clock in",
        Description = "Opens a new attendance session using the external time provider. Fails if a session is already open.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Clocked in successfully.",         typeof(AttendanceRecordDto))]
    [SwaggerResponse(StatusCodes.Status409Conflict,     "Employee is already clocked in.",  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClockIn(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.ClockInAsync(employeeId, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/clock-in succeeded. EmployeeId={EmployeeId} RecordId={RecordId}",
            employeeId, result.Id);

        return Ok(result);
    }

    /// <summary>
    /// Records a clock-out event for the authenticated employee's active session.
    /// The timestamp is sourced from the external time provider (Europe/Zurich).
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated attendance record with clock-out time and duration.</returns>
    [HttpPost("clock-out")]
    [SwaggerOperation(
        Summary = "Clock out",
        Description = "Closes the active attendance session using the external time provider. Fails if no session is open.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Clocked out successfully.",        typeof(AttendanceRecordDto))]
    [SwaggerResponse(StatusCodes.Status409Conflict,     "No active session to close.",      typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClockOut(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.ClockOutAsync(employeeId, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/clock-out succeeded. EmployeeId={EmployeeId} Duration={Duration}",
            employeeId, result.Duration);

        return Ok(result);
    }

    /// <summary>
    /// Returns the authenticated employee's current attendance status,
    /// including elapsed time if they are clocked in.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Real-time status with optional active session details.</returns>
    [HttpGet("status")]
    [SwaggerOperation(
        Summary = "Get current status",
        Description = "Returns whether the employee is clocked in and, if so, the active record details and elapsed duration.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Status retrieved successfully.",   typeof(CurrentAttendanceStatusDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(CurrentAttendanceStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),             StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.GetCurrentStatusAsync(employeeId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated attendance history for the authenticated employee,
    /// ordered by clock-in time descending.
    /// </summary>
    /// <param name="pageNumber">1-based page number (default: 1).</param>
    /// <param name="pageSize">Records per page, max 100 (default: 20).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Paginated list of attendance records with total count and page metadata.</returns>
    [HttpGet("history")]
    [SwaggerOperation(
        Summary = "Get attendance history",
        Description = "Returns a server-side paginated list of the employee's attendance records, most recent first.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "History retrieved successfully.",  typeof(PagedResult<AttendanceRecordDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Invalid pagination parameters.",   typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(PagedResult<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),                   StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),                   StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)]          int pageSize   = 20,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.GetAttendanceHistoryAsync(
            employeeId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Calculates the total hours worked during the current ISO week (Monday–Sunday)
    /// for the authenticated employee.
    /// Active sessions contribute their elapsed time up to the current moment.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Total weekly worked hours in multiple formats.</returns>
    [HttpGet("weekly-hours")]
    [SwaggerOperation(
        Summary = "Get weekly hours",
        Description = "Returns total hours worked in the current ISO week (Monday 00:00 – Sunday 23:59). Active sessions count up to the current moment.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Weekly hours calculated.",         typeof(WorkedHoursDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(WorkedHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWeeklyHours(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var duration = await _attendanceService.GetWeeklyHoursAsync(employeeId, cancellationToken);
        return Ok(WorkedHoursDto.FromTimeSpan(duration));
    }

    /// <summary>
    /// Calculates the total hours worked during the current calendar month
    /// for the authenticated employee.
    /// Active sessions contribute their elapsed time up to the current moment.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Total monthly worked hours in multiple formats.</returns>
    [HttpGet("monthly-hours")]
    [SwaggerOperation(
        Summary = "Get monthly hours",
        Description = "Returns total hours worked in the current calendar month. Active sessions count up to the current moment.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Monthly hours calculated.",        typeof(WorkedHoursDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(WorkedHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyHours(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var duration = await _attendanceService.GetMonthlyHoursAsync(employeeId, cancellationToken);
        return Ok(WorkedHoursDto.FromTimeSpan(duration));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Extracts and parses the employee ID from the <c>sub</c> JWT claim
    /// (mapped to <see cref="ClaimTypes.NameIdentifier"/> by the JWT middleware).
    /// </summary>
    /// <exception cref="AuthenticationException">Claim is missing or not a valid integer.</exception>
    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !int.TryParse(value, out var userId))
        {
            throw new AuthenticationException(
                "The authenticated token does not contain a valid user identifier.");
        }

        return userId;
    }
}
