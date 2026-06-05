namespace Attendance.Application.DTOs;

/// <summary>
/// Request payload for the token-refresh endpoint.
/// Both tokens must originate from the same prior authentication response.
/// </summary>
public sealed record RefreshTokenRequestDto
{
    /// <summary>
    /// Gets the JWT access token (may be expired).
    /// Its signature and structure are validated server-side to prevent token injection.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the opaque refresh token issued during login or a previous refresh.
    /// Single-use: it is revoked and replaced on every successful exchange.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
