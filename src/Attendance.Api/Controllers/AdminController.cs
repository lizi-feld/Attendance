using System.ComponentModel.DataAnnotations;
using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Administrative endpoints for managing employees and viewing the attendance dashboard.
/// All routes require the <c>Admin</c> role.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public sealed class AdminController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AdminController"/>.
    /// </summary>
    public AdminController(
        IAttendanceService attendanceService,
        IEmployeeService employeeService,
        ILogger<AdminController> logger)
    {
        _attendanceService = attendanceService;
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Returns a real-time dashboard snapshot: total employees, currently clocked-in count,
    /// today's session count, and the list of active sessions.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Dashboard summary with live attendance metrics.</returns>
    [HttpGet("dashboard")]
    [SwaggerOperation(
        Summary = "Get dashboard summary",
        Description = "Returns live attendance metrics: total employees, active sessions, and today's record count. " +
                      "Timestamps are in Europe/Zurich time sourced from the external time provider.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Dashboard data retrieved.",        typeof(DashboardSummaryDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Admin role required.",             typeof(ProblemDetails))]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _attendanceService.GetDashboardSummaryAsync(cancellationToken);

        _logger.LogInformation(
            "GET /api/admin/dashboard. TotalEmployees={Total} ClockedIn={ClockedIn}",
            result.TotalEmployees, result.ClockedInNow);

        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated, alphabetically sorted list of all registered employees.
    /// </summary>
    /// <param name="pageNumber">1-based page number (default: 1).</param>
    /// <param name="pageSize">Records per page, max 100 (default: 20).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Paginated employee list with total count and page metadata.</returns>
    [HttpGet("employees")]
    [SwaggerOperation(
        Summary = "List employees",
        Description = "Returns a server-side paginated, name-sorted list of all employees. Does not include attendance history.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Employee list retrieved.",         typeof(PagedResult<EmployeeDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Invalid pagination parameters.",   typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Admin role required.",             typeof(ProblemDetails))]
    [ProducesResponseType(typeof(PagedResult<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),           StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),           StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),           StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployees(
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)]          int pageSize   = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetAllPagedAsync(pageNumber, pageSize, cancellationToken);

        _logger.LogInformation(
            "GET /api/admin/employees. Page={Page} Size={Size} Total={Total}",
            pageNumber, pageSize, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Returns the full profile and complete attendance history for a specific employee.
    /// </summary>
    /// <param name="id">The employee's unique database identifier.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Employee details including all attendance records, most recent first.</returns>
    [HttpGet("employees/{id:int}")]
    [SwaggerOperation(
        Summary = "Get employee details",
        Description = "Returns an employee's full profile including their complete attendance history ordered by clock-in time descending.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Employee found.",                  typeof(EmployeeDetailsDto))]
    [SwaggerResponse(StatusCodes.Status404NotFound,     "Employee not found.",              typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Admin role required.",             typeof(ProblemDetails))]
    [ProducesResponseType(typeof(EmployeeDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),     StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails),     StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),     StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmployeeById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByIdAsync(id, cancellationToken);

        _logger.LogInformation(
            "GET /api/admin/employees/{EmployeeId}. Username={Username}",
            id, result.Username);

        return Ok(result);
    }
}
