using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IAbsenceService"/>: validates and persists employee absence reports.
/// </summary>
/// <remarks>
/// <b>Time rule:</b> every timestamp originates exclusively from <see cref="ITimeProvider"/>.
/// <see cref="DateTime.Now"/> and <see cref="DateTime.UtcNow"/> are never used in this class.
/// </remarks>
public sealed class AbsenceService : IAbsenceService
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IValidator<ReportAbsenceRequestDto> _reportAbsenceValidator;
    private readonly ILogger<AbsenceService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AbsenceService"/> with all required dependencies.
    /// </summary>
    public AbsenceService(
        IAbsenceRepository absenceRepository,
        IEmployeeRepository employeeRepository,
        ITimeProvider timeProvider,
        IValidator<ReportAbsenceRequestDto> reportAbsenceValidator,
        ILogger<AbsenceService> logger)
    {
        _absenceRepository = absenceRepository;
        _employeeRepository = employeeRepository;
        _timeProvider = timeProvider;
        _reportAbsenceValidator = reportAbsenceValidator;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<AbsenceRecordDto> ReportAbsenceAsync(
        int employeeId,
        ReportAbsenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _reportAbsenceValidator.ValidateAndThrowAsync(request, cancellationToken);

        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);

        var record = AbsenceRecord.Create(
            employeeId,
            request.Date,
            request.AbsenceType,
            request.DocumentUrl,
            request.Note,
            now);

        var persisted = await _absenceRepository.AddAsync(record, cancellationToken);

        _logger.LogInformation(
            "Absence reported. EmployeeId={EmployeeId} Username={Username} Date={Date} Type={Type}",
            employeeId, employee.Username, request.Date, request.AbsenceType);

        return MapToDto(persisted);
    }

    /// <inheritdoc />
    public async Task<int?> GetOwningEmployeeIdForDocumentAsync(
        string documentUrl,
        CancellationToken cancellationToken = default)
    {
        var record = await _absenceRepository.GetByDocumentUrlAsync(documentUrl, cancellationToken);
        return record?.EmployeeId;
    }

    // ── Private mapping helpers ──────────────────────────────────────────────

    private static AbsenceRecordDto MapToDto(AbsenceRecord record) => new()
    {
        Id               = record.Id,
        EmployeeId       = record.EmployeeId,
        EmployeeFullName = record.Employee?.FullName ?? string.Empty,
        Date             = record.Date,
        AbsenceType      = record.Type.ToString(),
        DocumentUrl      = record.DocumentUrl,
        Note             = record.Note,
        CreatedAt        = record.CreatedAt
    };
}
