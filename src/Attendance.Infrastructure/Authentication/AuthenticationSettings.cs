namespace Attendance.Infrastructure.Authentication;

/// <summary>
/// Strongly-typed configuration for JWT and refresh token generation.
/// Bound from the <c>"Authentication"</c> section of <c>appsettings.json</c>.
/// </summary>
/// <example>
/// <code>
/// "Authentication": {
///   "Issuer": "https://attendance.example.com",
///   "Audience": "attendance-clients",
///   "SecretKey": "&lt;min-32-char-secret&gt;",
///   "AccessTokenExpirationMinutes": 60,
///   "RefreshTokenExpirationDays": 7
/// }
/// </code>
/// </example>
public sealed class AuthenticationSettings
{
    /// <summary>The configuration section key used for <c>IOptions</c> binding.</summary>
    public const string SectionName = "Authentication";

    /// <summary>Gets the token issuer (the authority that minted the JWT).</summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets the intended audience for the JWT (typically the API base URL or client identifier).</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the HMAC-SHA256 signing secret.
    /// Must be at least 32 characters long. Store in a secret store (Azure Key Vault, environment variable) — never in source control.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>Gets the JWT access token lifetime in minutes. Default: 60.</summary>
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    /// <summary>Gets the refresh token lifetime in days. Default: 7.</summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
