namespace Attendance.Application.DTOs;

/// <summary>
/// Response payload returned upon successful employee authentication.
/// </summary>
public sealed record LoginResponse
{
    /// <summary>Gets the signed JWT Bearer token for subsequent authenticated requests.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>Gets the token scheme. Always <c>"Bearer"</c>.</summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>Gets the UTC timestamp at which the token expires.</summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Gets the opaque, cryptographically random refresh token.
    /// Store client-side in a secure, HttpOnly cookie.
    /// Use it with <c>POST /auth/refresh</c> to obtain new access tokens.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Gets the authenticated employee's basic profile information.</summary>
    public EmployeeDto Employee { get; init; } = null!;
}
