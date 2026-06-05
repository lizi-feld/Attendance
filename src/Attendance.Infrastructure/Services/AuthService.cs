using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IAuthService"/>: login, token refresh, token revocation,
/// and password hashing delegated to <see cref="IPasswordHashingService"/>.
/// </summary>
/// <remarks>
/// <para><b>Refresh token rotation:</b> every successful <see cref="RefreshTokenAsync"/> call
/// revokes the submitted refresh token and issues a brand-new one (single-use enforcement).</para>
/// <para><b>Time source:</b> <see cref="ITimeProvider"/> is used for all token timestamps
/// (refresh token <c>ExpiresAt</c>, <c>CreatedAt</c>, <c>RevokedAt</c>).
/// JWT <c>exp</c> is UTC-based per the JWT specification and uses <c>DateTime.UtcNow</c>.</para>
/// </remarks>
public sealed class AuthService : IAuthService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITimeProvider _timeProvider;
    private readonly AuthenticationSettings _settings;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthService"/> with all required dependencies.
    /// </summary>
    public AuthService(
        IEmployeeRepository employeeRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHashingService passwordHashingService,
        IJwtTokenService jwtTokenService,
        ITimeProvider timeProvider,
        IOptions<AuthenticationSettings> settings,
        ILogger<AuthService> logger)
    {
        _employeeRepository = employeeRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHashingService = passwordHashingService;
        _jwtTokenService = jwtTokenService;
        _timeProvider = timeProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidCredentialsException">Username not found or password incorrect.</exception>
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        // Username is stored lowercase; normalise before lookup.
        var employee = await _employeeRepository.GetByUsernameAsync(
            request.Username.Trim().ToLowerInvariant(), cancellationToken);

        if (employee is null || !_passwordHashingService.VerifyPassword(request.Password, employee.PasswordHash))
        {
            // Single log — does not reveal whether the username or password was wrong.
            _logger.LogWarning(
                "Failed login attempt. Username={Username}",
                request.Username.Trim().ToLowerInvariant());

            throw new InvalidCredentialsException();
        }

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(employee);
        var refreshTokenValue = GenerateRefreshToken();
        var refreshTokenExpiresAt = now.AddDays(_settings.RefreshTokenExpirationDays);

        var refreshToken = RefreshToken.Create(refreshTokenValue, employee.Id, refreshTokenExpiresAt, now);
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        _logger.LogInformation(
            "Login successful. EmployeeId={EmployeeId} Username={Username} RefreshTokenId={RefreshTokenId}",
            employee.Id, employee.Username, refreshToken.Id);

        return new LoginResponse
        {
            Token = accessToken,
            TokenType = "Bearer",
            ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            RefreshToken = refreshTokenValue,
            Employee = MapToEmployeeDto(employee)
        };
    }

    /// <inheritdoc />
    /// <exception cref="AuthenticationException">
    /// Access token is structurally invalid; refresh token not found, expired, revoked,
    /// or belongs to a different employee than the access token.
    /// </exception>
    public async Task<RefreshTokenResponseDto> RefreshTokenAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Validate the access token's structure and signature (lifetime ignored intentionally).
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            _logger.LogWarning("Refresh rejected: access token failed structural validation.");
            throw new AuthenticationException("The provided access token is invalid.");
        }

        // Step 2: Extract the employee ID embedded in the access token.
        // Default JWT claim mapping: 'sub' → ClaimTypes.NameIdentifier.
        var subClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!int.TryParse(subClaim, out var claimEmployeeId))
        {
            _logger.LogWarning("Refresh rejected: access token missing or invalid 'sub' claim.");
            throw new AuthenticationException("The access token does not contain a valid employee identifier.");
        }

        // Step 3: Look up the submitted refresh token.
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (storedToken is null)
        {
            _logger.LogWarning(
                "Refresh rejected: refresh token not found. ClaimEmployeeId={EmployeeId}",
                claimEmployeeId);
            throw new AuthenticationException("The provided refresh token was not found.");
        }

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);

        // Step 4: Validate liveness and cross-validate token ownership.
        if (!storedToken.IsActiveAt(now))
        {
            _logger.LogWarning(
                "Refresh rejected: token is expired or revoked. TokenId={TokenId} EmployeeId={EmployeeId}",
                storedToken.Id, storedToken.EmployeeId);
            throw new AuthenticationException("The refresh token is expired or has been revoked.");
        }

        if (storedToken.EmployeeId != claimEmployeeId)
        {
            // Possible token-injection attack — log with high severity.
            _logger.LogWarning(
                "Refresh rejected: token/claim employee mismatch. TokenEmployeeId={TokenEmpId} ClaimEmployeeId={ClaimEmpId}",
                storedToken.EmployeeId, claimEmployeeId);
            throw new AuthenticationException("The refresh token does not match the supplied access token.");
        }

        // Step 5: Fetch the current employee record.
        var employee = await _employeeRepository.GetByIdAsync(storedToken.EmployeeId, cancellationToken)
            ?? throw new AuthenticationException("The employee associated with this token no longer exists.");

        // Step 6: Revoke the consumed token (single-use enforcement).
        storedToken.Revoke(now);
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        // Step 7: Issue new token pair.
        var newAccessToken = _jwtTokenService.GenerateAccessToken(employee);
        var newRefreshTokenValue = GenerateRefreshToken();
        var newRefreshToken = RefreshToken.Create(
            newRefreshTokenValue,
            employee.Id,
            now.AddDays(_settings.RefreshTokenExpirationDays),
            now);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        _logger.LogInformation(
            "Refresh token rotated. EmployeeId={EmployeeId} OldTokenId={OldId} NewTokenId={NewId}",
            employee.Id, storedToken.Id, newRefreshToken.Id);

        return new RefreshTokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshTokenValue
        };
    }

    /// <inheritdoc />
    /// <exception cref="AuthenticationException">Refresh token not found.</exception>
    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken, cancellationToken)
            ?? throw new AuthenticationException("The provided refresh token was not found.");

        if (storedToken.IsRevoked)
        {
            // Idempotent: already revoked, nothing to do.
            _logger.LogWarning(
                "Revocation requested for already-revoked token. TokenId={TokenId} EmployeeId={EmployeeId}",
                storedToken.Id, storedToken.EmployeeId);
            return;
        }

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        storedToken.Revoke(now);
        await _refreshTokenRepository.UpdateAsync(storedToken, cancellationToken);

        _logger.LogInformation(
            "Refresh token revoked. TokenId={TokenId} EmployeeId={EmployeeId}",
            storedToken.Id, storedToken.EmployeeId);
    }

    /// <inheritdoc />
    public string HashPassword(string password) =>
        _passwordHashingService.HashPassword(password);

    /// <inheritdoc />
    public bool VerifyPassword(string password, string hash) =>
        _passwordHashingService.VerifyPassword(password, hash);

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Generates a cryptographically random, URL-safe refresh token.
    /// Uses <see cref="RandomNumberGenerator.GetBytes(int)"/> for 512 bits of entropy (64 bytes).
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    private static EmployeeDto MapToEmployeeDto(Employee employee) => new()
    {
        Id = employee.Id,
        Username = employee.Username,
        FullName = employee.FullName,
        Role = employee.Role.ToString(),
        CreatedAt = employee.CreatedAt
    };
}
