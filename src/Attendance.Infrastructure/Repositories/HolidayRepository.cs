using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IHolidayRepository"/>.
/// </summary>
public sealed class HolidayRepository : IHolidayRepository
{
    private readonly AttendanceDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="HolidayRepository"/>
    /// with the injected <see cref="AttendanceDbContext"/>.
    /// </summary>
    public HolidayRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var (yearStart, yearEnd) = YearBounds(year);

        return await _context.HolidayRecords
            .AsNoTracking()
            .AnyAsync(h => h.Date >= yearStart && h.Date < yearEnd, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HolidayRecord>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
    {
        var (yearStart, yearEnd) = YearBounds(year);

        return await _context.HolidayRecords
            .AsNoTracking()
            .Where(h => h.Date >= yearStart && h.Date < yearEnd)
            .OrderBy(h => h.Date)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<HolidayRecord> records, CancellationToken cancellationToken = default)
    {
        await _context.HolidayRecords.AddRangeAsync(records, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static (DateOnly Start, DateOnly End) YearBounds(int year)
    {
        var start = new DateOnly(year, 1, 1);
        return (start, start.AddYears(1));
    }
}
