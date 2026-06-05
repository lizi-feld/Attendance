using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Attendance.Infrastructure.Authentication;

/// <summary>
/// Implements <see cref="IJwtTokenService"/> using <see cref="JwtSecurityTokenHandler"/>.
/// Generates HMAC-SHA256-signed access tokens and validates their structure for the refresh flow.
/// </summary>
/// <remarks>
/// <para>
/// JWT expiry is computed using <see cref="DateTime.UtcNow"/> — this is intentional and correct.
/// The <c>exp</c> claim in the JWT standard is always UTC-epoch based and is independent
/// of the attendance time provider constraint (which applies only to clock-in/out records).
/// </para>
/// <para>
/// The signing key is pre-computed once in the constructor and reused for every token operation.
/// </para>
/// </remarks>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly AuthenticationSettings _settings;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TokenValidationParameters _refreshValidationParameters;

    /// <summary>
    /// Initializes a new instance of <see cref="JwtTokenService"/> and pre-builds
    /// the signing key and refresh-flow validation parameters from <paramref name="settings"/>.
    /// </summary>
    /// <param name="settings">Bound configuration from the <c>Authentication</c> appsettings section.</param>
    public JwtTokenService(IOptions<AuthenticationSettings> settings)
    {
        _settings = settings.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));

        // Pre-built for the refresh flow: validates everything except token lifetime.
        _refreshValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateLifetime = false,            // Expired tokens are valid input for refresh.
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Embedded claims:
    /// <list type="bullet">
    /// <item><c>sub</c> — employee database ID (maps to <see cref="ClaimTypes.NameIdentifier"/>)</item>
    /// <item><c>unique_name</c> — lowercase username (maps to <see cref="ClaimTypes.Name"/>)</item>
    /// <item><see cref="ClaimTypes.Role"/> — role enum name (e.g., <c>"Admin"</c>)</item>
    /// <item><c>jti</c> — unique token ID (prevents replay within the token lifetime)</item>
    /// <item><c>iat</c> — issued-at epoch timestamp</item>
    /// </list>
    /// </remarks>
    public string GenerateAccessToken(Employee employee)
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, employee.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, employee.Username),
            new(ClaimTypes.Role, employee.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler()
                .ValidateToken(accessToken, _refreshValidationParameters, out var validatedToken);

            // Guard against non-JWT or wrong algorithm tokens slipping through.
            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
