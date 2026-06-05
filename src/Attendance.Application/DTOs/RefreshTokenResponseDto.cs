namespace Attendance.Application.DTOs;

/// <summary>
/// Response payload returned after a successful token refresh.
/// Replace both tokens on the client simultaneously.
/// </summary>
public sealed record RefreshTokenResponseDto
{
    /// <summary>Gets the newly issued JWT Bearer access token.</summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Gets the newly issued opaque refresh token.
    /// The previous refresh token has been permanently revoked.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
