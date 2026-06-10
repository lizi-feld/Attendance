using Attendance.Application.Commands;
using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using FluentValidation;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IEmployeeService"/>: employee creation, update, and read projections.
/// Domain entities are never exposed beyond this class — all public methods return DTOs.
/// </summary>
public sealed class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ITimeProvider _timeProvider;
    private readonly IValidator<CreateEmployeeCommand> _createValidator;
    private readonly IValidator<UpdateEmployeeCommand> _updateValidator;

    /// <summary>
    /// Initializes a new instance of <see cref="EmployeeService"/>.
    /// </summary>
    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IPasswordHashingService passwordHashingService,
        ITimeProvider timeProvider,
        IValidator<CreateEmployeeCommand> createValidator,
        IValidator<UpdateEmployeeCommand> updateValidator)
    {
        _employeeRepository = employeeRepository;
        _passwordHashingService = passwordHashingService;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <inheritdoc />
    /// <exception cref="UsernameAlreadyExistsException">Username is already taken.</exception>
    public async Task<EmployeeDto> CreateAsync(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var normalizedUsername = command.Username.Trim().ToLowerInvariant();
        if (await _employeeRepository.ExistsAsync(normalizedUsername, cancellationToken))
            throw new UsernameAlreadyExistsException(command.Username);

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var passwordHash = _passwordHashingService.HashPassword(command.Password);
        var employee = Employee.Create(normalizedUsername, passwordHash, command.FullName, command.Role, now);
        var created = await _employeeRepository.AddAsync(employee, cancellationToken);

        return MapToDto(created);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    /// <exception cref="UsernameAlreadyExistsException">Requested new username is already taken.</exception>
    public async Task<EmployeeDto> UpdateAsync(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var employee = await _employeeRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new EmployeeNotFoundException(command.Id);

        if (command.FullName is not null)
            employee.UpdateFullName(command.FullName);

        if (command.Username is not null)
        {
            var normalizedUsername = command.Username.Trim().ToLowerInvariant();
            if (normalizedUsername != employee.Username &&
                await _employeeRepository.ExistsAsync(normalizedUsername, cancellationToken))
                throw new UsernameAlreadyExistsException(command.Username);

            employee.UpdateUsername(command.Username);
        }

        if (command.Password is not null)
            employee.UpdatePasswordHash(_passwordHashingService.HashPassword(command.Password));

        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        return MapToDto(employee);
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
