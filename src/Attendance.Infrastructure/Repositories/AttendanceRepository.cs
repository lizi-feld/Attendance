using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAttendanceRepository"/>.
/// All read operations use <c>AsNoTracking</c> and eagerly load the <see cref="Employee"/>
/// navigation property so callers always have access to <c>Employee.FullName</c> for DTO mapping.
/// </summary>
public sealed class AttendanceRepository : IAttendanceRepository
{
    private readonly AttendanceDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceRepository"/>
    /// with the injected <see cref="AttendanceDbContext"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public AttendanceRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AttendanceRecord?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Filters on <c>ClockOutTime IS NULL</c> to find the open session.
    /// The composite index <c>IX_AttendanceRecords_EmployeeId_ClockOutTime</c>
    /// makes this the fastest query in the attendance flow.
    /// </remarks>
    public async Task<AttendanceRecord?> GetActiveRecordAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.ClockOutTime == null, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Results are ordered by <see cref="AttendanceRecord.ClockInTime"/> descending (most recent first).</remarks>
    public async Task<IReadOnlyList<AttendanceRecord>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.ClockInTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Results are ordered by <see cref="AttendanceRecord.ClockInTime"/> ascending to show earliest sessions first on the dashboard.</remarks>
    public async Task<IReadOnlyList<AttendanceRecord>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.ClockOutTime == null)
            .OrderBy(a => a.ClockInTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses a half-open date range <c>[startOfDay, startOfNextDay)</c> against <see cref="AttendanceRecord.ClockInTime"/>
    /// so the query utilises <c>IX_AttendanceRecords_ClockInTime</c> efficiently without
    /// per-row date-part extraction functions.
    /// The date values must be in Europe/Zurich time to match the stored clock-in timestamps.
    /// </remarks>
    public async Task<IReadOnlyList<AttendanceRecord>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.ToDateTime(TimeOnly.MinValue);
        var startOfNextDay = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.ClockInTime >= startOfDay && a.ClockInTime < startOfNextDay)
            .OrderByDescending(a => a.ClockInTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// After persisting, the entity is reloaded with <c>Include(Employee)</c>
    /// so the caller always receives a fully populated record suitable for DTO mapping.
    /// </remarks>
    public async Task<AttendanceRecord> AddAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .FirstAsync(a => a.Id == record.Id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AttendanceRecord>> GetByDateRangeAsync(
        int employeeId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceRecords
            .AsNoTracking()
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId
                     && a.ClockInTime >= from
                     && a.ClockInTime < to)
            .OrderByDescending(a => a.ClockInTime)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Issues two queries: one for the page slice (Skip/Take) and one for the total count,
    /// both against the same filtered set. EF Core translates both to efficient SQL.
    /// </remarks>
    public async Task<(IReadOnlyList<AttendanceRecord> Records, int TotalCount)> GetPagedByEmployeeIdAsync(
        int employeeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var records = await baseQuery
            .Include(a => a.Employee)
            .OrderByDescending(a => a.ClockInTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (records, totalCount);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <c>Entry().State = Modified</c> rather than <c>Update()</c> to avoid
    /// accidentally cascading a state change to the <see cref="Employee"/> navigation property.
    /// Caller is responsible for invoking domain methods (e.g. <c>ClockOut</c>) before calling this.
    /// </remarks>
    public async Task UpdateAsync(AttendanceRecord record, CancellationToken cancellationToken = default)
    {
        _context.Entry(record).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
