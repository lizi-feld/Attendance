using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAbsenceRepository"/>.
/// </summary>
public sealed class AbsenceRepository : IAbsenceRepository
{
    private readonly AttendanceDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="AbsenceRepository"/>
    /// with the injected <see cref="AttendanceDbContext"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public AbsenceRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    /// <remarks>
    /// After persisting, the entity is reloaded with <c>Include(Employee)</c>
    /// so the caller always receives a fully populated record suitable for DTO mapping.
    /// </remarks>
    public async Task<AbsenceRecord> AddAsync(AbsenceRecord record, CancellationToken cancellationToken = default)
    {
        await _context.AbsenceRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.AbsenceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .FirstAsync(a => a.Id == record.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AbsenceRecord?> GetByDocumentUrlAsync(string documentUrl, CancellationToken cancellationToken = default)
    {
        return await _context.AbsenceRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DocumentUrl == documentUrl, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AbsenceRecord>> GetByEmployeeIdAndDateRangeAsync(
        int employeeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return await _context.AbsenceRecords
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Date >= from && a.Date < to)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
