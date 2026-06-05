using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Implements attendance business logic: clock-in/out, status queries, history, and hour summaries.
/// </summary>
/// <remarks>
/// <para><b>Time rule:</b> every timestamp originates exclusively from <see cref="ITimeProvider"/>.
/// <see cref="DateTime.Now"/> and <see cref="DateTime.UtcNow"/> are never used in this class.</para>
/// <para><b>Business rules enforced:</b>
/// <list type="bullet">
/// <item>An employee may have at most one active shift at any time.</item>
/// <item>Clock-in is rejected when an active shift already exists.</item>
/// <item>Clock-out is rejected when no active shift exists.</item>
/// <item>Duration calculations for active sessions use the live time from <see cref="ITimeProvider"/>.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AttendanceService : IAttendanceService
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly ILogger<AttendanceService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceService"/> with all required dependencies.
    /// </summary>
    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IEmployeeRepository employeeRepository,
        ITimeProvider timeProvider,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _employeeRepository = employeeRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    /// <exception cref="ActiveShiftAlreadyExistsException">Employee is already clocked in.</exception>
    public async Task<AttendanceRecordDto> ClockInAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var existing = await _attendanceRepository.GetActiveRecordAsync(employeeId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogWarning(
                "Clock-in rejected. EmployeeId={EmployeeId} Username={Username} ActiveRecordId={RecordId}",
                employeeId, employee.Username, existing.Id);

            throw new ActiveShiftAlreadyExistsException(employeeId, existing.Id);
        }

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var record = AttendanceRecord.Create(employeeId, now, now);
        var persisted = await _attendanceRepository.AddAsync(record, cancellationToken);

        _logger.LogInformation(
            "Clock-in successful. EmployeeId={EmployeeId} Username={Username} ClockInTime={ClockInTime} RecordId={RecordId}",
            employeeId, employee.Username, now, persisted.Id);

        return MapToDto(persisted);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    /// <exception cref="ActiveShiftNotFoundException">No active shift to close.</exception>
    public async Task<AttendanceRecordDto> ClockOutAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var activeRecord = await _attendanceRepository.GetActiveRecordAsync(employeeId, cancellationToken)
            ?? throw new ActiveShiftNotFoundException(employeeId);

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);

        // Domain method enforces invariants: session must be open, clock-out must be after clock-in.
        activeRecord.ClockOut(now);
        await _attendanceRepository.UpdateAsync(activeRecord, cancellationToken);

        _logger.LogInformation(
            "Clock-out successful. EmployeeId={EmployeeId} Username={Username} ClockOutTime={ClockOutTime} Duration={Duration} RecordId={RecordId}",
            employeeId, employee.Username, now, activeRecord.Duration, activeRecord.Id);

        return MapToDto(activeRecord);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<CurrentAttendanceStatusDto> GetCurrentStatusAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var activeRecord = await _attendanceRepository.GetActiveRecordAsync(employeeId, cancellationToken);

        if (activeRecord is null)
        {
            return new CurrentAttendanceStatusDto { IsClockedIn = false };
        }

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);

        return new CurrentAttendanceStatusDto
        {
            IsClockedIn = true,
            ActiveRecordId = activeRecord.Id,
            ClockInTime = activeRecord.ClockInTime,
            CurrentDuration = now - activeRecord.ClockInTime
        };
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<PagedResult<AttendanceRecordDto>> GetAttendanceHistoryAsync(
        int employeeId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var (records, totalCount) = await _attendanceRepository.GetPagedByEmployeeIdAsync(
            employeeId, pageNumber, pageSize, cancellationToken);

        var items = records.Select(MapToDto).ToList();

        return PagedResult<AttendanceRecordDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Week boundary: Monday 00:00:00 (inclusive) through the start of the following Monday (exclusive).
    /// Any active session's end time is treated as the current moment from <see cref="ITimeProvider"/>.
    /// </remarks>
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<TimeSpan> GetWeeklyHoursAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var (weekStart, weekEnd) = GetCurrentWeekBounds(now);

        var records = await _attendanceRepository.GetByDateRangeAsync(
            employeeId, weekStart, weekEnd, cancellationToken);

        return CalculateTotalDuration(records, now);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Month boundary: first day of the current month at 00:00:00 (inclusive) through the first
    /// day of the next month at 00:00:00 (exclusive).
    /// Any active session's end time is treated as the current moment from <see cref="ITimeProvider"/>.
    /// </remarks>
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<TimeSpan> GetMonthlyHoursAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var (monthStart, monthEnd) = GetCurrentMonthBounds(now);

        var records = await _attendanceRepository.GetByDateRangeAsync(
            employeeId, monthStart, monthEnd, cancellationToken);

        return CalculateTotalDuration(records, now);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<IReadOnlyList<AttendanceRecordDto>> GetRecordsAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var records = await _attendanceRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
        return records.Select(MapToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var today = DateOnly.FromDateTime(now);

        var allEmployees = await _employeeRepository.GetAllAsync(cancellationToken);
        var activeSessions = await _attendanceRepository.GetAllActiveAsync(cancellationToken);
        var todayRecords = await _attendanceRepository.GetByDateAsync(today, cancellationToken);

        return new DashboardSummaryDto
        {
            TotalEmployees = allEmployees.Count,
            ClockedInNow = activeSessions.Count,
            TotalRecordsToday = todayRecords.Count,
            ActiveSessions = activeSessions.Select(MapToDto).ToList(),
            GeneratedAt = now
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static AttendanceRecordDto MapToDto(AttendanceRecord record) => new()
    {
        Id = record.Id,
        EmployeeId = record.EmployeeId,
        EmployeeFullName = record.Employee?.FullName ?? string.Empty,
        ClockInTime = record.ClockInTime,
        ClockOutTime = record.ClockOutTime,
        Duration = record.Duration,
        CreatedAt = record.CreatedAt
    };

    /// <summary>
    /// Returns the Monday–Sunday bounds for the week containing <paramref name="now"/>.
    /// Range is half-open: <c>[monday, monday + 7 days)</c>.
    /// </summary>
    private static (DateTime WeekStart, DateTime WeekEnd) GetCurrentWeekBounds(DateTime now)
    {
        // DayOfWeek: Sunday=0, Monday=1 ... Saturday=6
        var dow = (int)now.DayOfWeek;
        var daysFromMonday = dow == 0 ? 6 : dow - 1;
        var monday = now.Date.AddDays(-daysFromMonday);
        return (monday, monday.AddDays(7));
    }

    /// <summary>
    /// Returns the calendar-month bounds for the month containing <paramref name="now"/>.
    /// Range is half-open: <c>[first of month, first of next month)</c>.
    /// </summary>
    private static (DateTime MonthStart, DateTime MonthEnd) GetCurrentMonthBounds(DateTime now)
    {
        var firstOfMonth = new DateTime(now.Year, now.Month, 1);
        return (firstOfMonth, firstOfMonth.AddMonths(1));
    }

    /// <summary>
    /// Sums the worked duration across all records.
    /// For sessions still active (no clock-out), uses <paramref name="now"/> as the end time.
    /// </summary>
    private static TimeSpan CalculateTotalDuration(IReadOnlyList<AttendanceRecord> records, DateTime now) =>
        records.Aggregate(
            TimeSpan.Zero,
            (total, r) => total + ((r.ClockOutTime ?? now) - r.ClockInTime));
}
