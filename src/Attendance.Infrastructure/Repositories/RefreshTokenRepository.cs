using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRefreshTokenRepository"/>.
/// All reads use <c>AsNoTracking</c>; writes use <c>Entry().State</c> to avoid
/// accidentally cascading state to the <see cref="Employee"/> navigation.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AttendanceDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="RefreshTokenRepository"/>
    /// with the injected <see cref="AttendanceDbContext"/>.
    /// </summary>
    public RefreshTokenRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .AsNoTracking()
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RefreshToken> AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <c>Entry().State = Modified</c> to avoid propagating state to the
    /// <see cref="Employee"/> navigation when only <c>RevokedAt</c> has changed.
    /// </remarks>
    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.Entry(refreshToken).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Loads active tokens for the employee, applies the domain <c>Revoke</c> method on each,
    /// then saves all changes in a single <c>SaveChangesAsync</c> call.
    /// </remarks>
    public async Task RevokeAllForEmployeeAsync(
        int employeeId,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(r => r.EmployeeId == employeeId && r.RevokedAt == null)
            .ToListAsync(cancellationToken);

        if (activeTokens.Count == 0)
            return;

        foreach (var token in activeTokens)
        {
            token.Revoke(revokedAt);
            _context.Entry(token).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses a bulk <c>ExecuteDeleteAsync</c> (EF Core 7+) for efficient deletion without
    /// loading entities into memory.
    /// </remarks>
    public async Task DeleteExpiredAsync(DateTime cutoff, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens
            .Where(r => r.ExpiresAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
