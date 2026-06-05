using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IEmployeeService"/> by projecting <see cref="Employee"/> domain entities
/// into read-only DTOs for the admin API surface.
/// </summary>
public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    /// <summary>
    /// Initializes a new instance of <see cref="EmployeeService"/>.
    /// </summary>
    /// <param name="employeeRepository">Data access abstraction for employee records.</param>
    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    /// <inheritdoc />
    public async Task<PagedResult<EmployeeDto>> GetAllPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (employees, totalCount) = await _employeeRepository.GetPagedAsync(
            pageNumber, pageSize, cancellationToken);

        var items = employees.Select(MapToDto).ToList();

        return PagedResult<EmployeeDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    public async Task<EmployeeDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new EmployeeNotFoundException(id);

        return MapToDetailsDto(employee);
    }

    // ── Private mapping helpers ──────────────────────────────────────────────

    private static EmployeeDto MapToDto(Employee employee) => new()
    {
        Id = employee.Id,
        Username = employee.Username,
        FullName = employee.FullName,
        Role = employee.Role.ToString(),
        CreatedAt = employee.CreatedAt
    };

    private static EmployeeDetailsDto MapToDetailsDto(Employee employee) => new()
    {
        Id = employee.Id,
        Username = employee.Username,
        FullName = employee.FullName,
        Role = employee.Role.ToString(),
        CreatedAt = employee.CreatedAt,
        AttendanceRecords = employee.AttendanceRecords
            .OrderByDescending(r => r.ClockInTime)
            .Select(r => MapRecordToDto(r, employee.FullName))
            .ToList()
    };

    // When loading records via Employee.AttendanceRecords, the reverse navigation
    // (AttendanceRecord.Employee) is not populated. We pass the parent employee's
    // name directly to avoid an N+1 or null-reference issue.
    private static AttendanceRecordDto MapRecordToDto(AttendanceRecord record, string employeeFullName) => new()
    {
        Id = record.Id,
        EmployeeId = record.EmployeeId,
        EmployeeFullName = employeeFullName,
        ClockInTime = record.ClockInTime,
        ClockOutTime = record.ClockOutTime,
        Duration = record.Duration,
        CreatedAt = record.CreatedAt
    };
}
