using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Handles employee authentication, JWT token refresh, and token revocation.
/// All endpoints are publicly accessible — no JWT is required to reach this controller.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthController"/>.
    /// </summary>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates an employee with username and password.
    /// On success returns a JWT access token and a single-use refresh token.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>JWT access token, refresh token, expiry, and employee profile.</returns>
    [HttpPost("login")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Login",
        Description = "Authenticates an employee and issues a JWT access token plus a cryptographically random refresh token.")]
    [SwaggerResponse(StatusCodes.Status200OK,          "Authentication successful.",         typeof(LoginResponse))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid username or password.",      typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,  "Request body failed validation.",    typeof(ProblemDetails))]
    [ProducesResponseType(typeof(LoginResponse),   StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),  StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),  StatusCodes.Status400BadRequest)]
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
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Refresh token",
        Description = "Rotates the token pair. The old refresh token is revoked immediately upon success.")]
    [SwaggerResponse(StatusCodes.Status200OK,          "Token pair rotated.",                    typeof(RefreshTokenResponseDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token is invalid, expired, or revoked.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,  "Request body failed validation.",        typeof(ProblemDetails))]
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
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Revoke refresh token",
        Description = "Permanently invalidates the provided refresh token. Use during user logout.")]
    [SwaggerResponse(StatusCodes.Status204NoContent,   "Token revoked successfully.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Token not found.",                  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,  "Request body failed validation.",   typeof(ProblemDetails))]
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
}
