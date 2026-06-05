using System.Security.Claims;
using Attendance.Domain.Entities;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines JWT access-token generation and structural validation.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT access token for the specified employee.
    /// The token embeds the employee's <c>Id</c>, <c>Username</c>, and <c>Role</c> as standard claims.
    /// </summary>
    /// <param name="employee">The authenticated employee whose claims are embedded in the token.</param>
    /// <returns>A compact, signed JWT string.</returns>
    string GenerateAccessToken(Employee employee);

    /// <summary>
    /// Validates an access token's signature, issuer, and audience without checking its lifetime,
    /// and returns the embedded <see cref="ClaimsPrincipal"/>.
    /// Used during the refresh-token flow to authenticate the caller even when the access token is expired.
    /// </summary>
    /// <param name="accessToken">The (possibly expired) JWT access token string.</param>
    /// <returns>
    /// The <see cref="ClaimsPrincipal"/> extracted from the token, or <c>null</c> when the token
    /// is structurally invalid (tampered signature, wrong issuer/audience, malformed JWT).
    /// </returns>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken);
}
