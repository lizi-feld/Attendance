using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Attendance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IEmployeeRepository"/>.
/// All read operations use <c>AsNoTracking</c> for performance.
/// Write operations attach the entity explicitly via <c>Update</c> or <c>Add</c>.
/// </summary>
public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AttendanceDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="EmployeeRepository"/>
    /// with the injected <see cref="AttendanceDbContext"/>.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    public EmployeeRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Eagerly loads <see cref="Employee.AttendanceRecords"/> to support
    /// the <c>EmployeeDetailsDto</c> projection in the service layer.
    /// </remarks>
    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.AttendanceRecords)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Performs a case-insensitive lookup against the lowercase-stored username column.
    /// Does not eagerly load attendance records — used primarily by the auth service.
    /// </remarks>
    public async Task<Employee?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLowerInvariant();

        return await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Username == normalized, cancellationToken);
    }

    /// <inheritdoc />
    /// <remarks>Results are ordered by <see cref="Employee.FullName"/> ascending.</remarks>
    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.FullName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return employee;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Attaches and marks the entity as modified regardless of tracking state,
    /// which handles the common pattern of loading with <c>AsNoTracking</c>
    /// and then mutating via a domain method before calling this.
    /// </remarks>
    /// <remarks>
    /// Uses <c>Entry().State = Modified</c> rather than <c>Update()</c> to avoid
    /// accidentally cascading a state change to the <see cref="Employee.AttendanceRecords"/> collection.
    /// </remarks>
    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        _context.Entry(employee).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLowerInvariant();

        return await _context.Employees
            .AnyAsync(e => e.Username == normalized, cancellationToken);
    }
}
