using Attendance.Application.Commands;
using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Handles employee authentication, JWT token lifecycle, and admin-only employee management.
/// Public endpoints (login, refresh, revoke) carry <c>[AllowAnonymous]</c> individually;
/// employee management endpoints require the <c>Admin</c> role.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    public AuthController(
        IAuthService authService,
        IEmployeeService employeeService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _employeeService = employeeService;
        _logger = logger;
    }

    // ── Public endpoints ─────────────────────────────────────────────────────

    /// <summary>
    /// Authenticates an employee with username and password.
    /// On success returns a JWT access token and a single-use refresh token.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>JWT access token, refresh token, expiry, and employee profile.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Login",
        Description = "Authenticates an employee and issues a JWT access token plus a cryptographically random refresh token.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Authentication successful.",        typeof(LoginResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid username or password.",     typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Request body failed validation.",   typeof(ProblemDetails))]
    [ProducesResponseType(typeof(LoginResponse),  StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);

        _logger.LogInformation(
            "POST /api/auth/login succeeded. EmployeeId={EmployeeId} Username={Username}",
            response.Employee.Id, response.Employee.Username);

        return Ok(response);
    }

    /// <summary>
    /// Exchanges a (possibly expired) access token and a valid refresh token for a new token pair.
    /// The submitted refresh token is immediately revoked (single-use rotation).
    /// </summary>
    /// <param name="request">The current access token and refresh token.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>New access token and rotated refresh token.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Refresh token",
        Description = "Rotates the token pair. The old refresh token is revoked immediately upon success.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Token pair rotated.",                    typeof(RefreshTokenResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token is invalid, expired, or revoked.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Request body failed validation.",        typeof(ProblemDetails))]
    [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),          StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),          StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await _authService.RefreshTokenAsync(request, cancellationToken);

        _logger.LogInformation("POST /api/auth/refresh-token succeeded.");

        return Ok(response);
    }

    /// <summary>
    /// Revokes a specific refresh token, preventing it from being used in future refresh requests.
    /// Use this on explicit user logout.
    /// </summary>
    /// <param name="request">The token pair containing the refresh token to revoke.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("revoke-token")]
    [AllowAnonymous]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Revoke refresh token",
        Description = "Permanently invalidates the provided refresh token. Use during user logout.")]
    [SwaggerResponse(StatusCodes.Status204NoContent,    "Token revoked successfully.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token not found.",                typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Request body failed validation.", typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken(
        [FromBody] RefreshTokenRequestDto request,
        CancellationToken cancellationToken)
    {
        await _authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        _logger.LogInformation("POST /api/auth/revoke-token succeeded.");

        return NoContent();
    }

    // ── Admin-only endpoints ─────────────────────────────────────────────────

    /// <summary>
    /// Creates a new employee account. Restricted to administrators.
    /// This is not a public registration endpoint.
    /// </summary>
    /// <param name="request">Employee details: full name, username, password, and optional role.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly created employee profile.</returns>
    [HttpPost("add-employee")]
    [Authorize(Roles = "Admin")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Add employee",
        Description = "Admin-only. Creates a new employee account with the specified credentials and role. " +
                      "Returns 409 if the username is already taken.")]
    [SwaggerResponse(StatusCodes.Status201Created,      "Employee created.",                typeof(EmployeeDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Request body failed validation.",  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Admin role required.",             typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status409Conflict,     "Username already taken.",          typeof(ProblemDetails))]
    [ProducesResponseType(typeof(EmployeeDto),  StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddEmployee(
        [FromBody] AddEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeCommand
        {
            FullName = request.FullName,
            Username = request.Username,
            Password = request.Password,
            Role     = request.Role
        };

        var employee = await _employeeService.CreateAsync(command, cancellationToken);

        _logger.LogInformation(
            "POST /api/auth/add-employee succeeded. EmployeeId={EmployeeId} Username={Username}",
            employee.Id, employee.Username);

        return CreatedAtAction(
            actionName:    nameof(AdminController.GetEmployeeById),
            controllerName: "Admin",
            routeValues:   new { id = employee.Id },
            value:         employee);
    }

    /// <summary>
    /// Updates an existing employee's details. Restricted to administrators.
    /// Only supplied (non-null) fields are modified; omitted fields remain unchanged.
    /// </summary>
    /// <param name="id">The employee's unique identifier.</param>
    /// <param name="request">Fields to update: full name, username, and/or password.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated employee profile.</returns>
    [HttpPut("update/{id:int}")]
    [Authorize(Roles = "Admin,Employee")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Update employee",
        Description = "Admin-only. Partially updates an employee's full name, username, and/or password. " +
                      "Omit a field (or set it to null) to leave it unchanged. " +
                      "Returns 409 if the new username is already taken by another employee.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Employee updated.",                typeof(EmployeeDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Request body failed validation.",  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Admin role required.",             typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound,     "Employee not found.",              typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status409Conflict,     "Username already taken.",          typeof(ProblemDetails))]
    [ProducesResponseType(typeof(EmployeeDto),  StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEmployee(
        [FromRoute] int id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeCommand
        {
            Id       = id,
            FullName = request.FullName,
            Username = request.Username,
            Password = request.Password
        };

        var employee = await _employeeService.UpdateAsync(command, cancellationToken);

        _logger.LogInformation(
            "PUT /api/auth/update/{EmployeeId} succeeded. Username={Username}",
            id, employee.Username);

        return Ok(employee);
    }
}
