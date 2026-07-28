using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Attendance.Domain.Entities;
using FluentValidation;
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
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly IValidator<ManualTimeUpdateRequestDto> _manualUpdateValidator;
    private readonly IValidator<ManualAddShiftRequestDto> _manualAddShiftValidator;
    private readonly ILogger<AttendanceService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceService"/> with all required dependencies.
    /// </summary>
    public AttendanceService(
        IAttendanceRepository attendanceRepository,
        IAbsenceRepository absenceRepository,
        IEmployeeRepository employeeRepository,
        ITimeProvider timeProvider,
        IValidator<ManualTimeUpdateRequestDto> manualUpdateValidator,
        IValidator<ManualAddShiftRequestDto> manualAddShiftValidator,
        ILogger<AttendanceService> logger)
    {
        _attendanceRepository = attendanceRepository;
        _absenceRepository = absenceRepository;
        _employeeRepository = employeeRepository;
        _timeProvider = timeProvider;
        _manualUpdateValidator = manualUpdateValidator;
        _manualAddShiftValidator = manualAddShiftValidator;
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
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        _ = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);

        var (records, totalCount) = await _attendanceRepository.GetPagedByEmployeeIdAsync(
            employeeId, pageNumber, pageSize, year, month, cancellationToken);

        var items = records.Select(MapToDto).ToList();

        return PagedResult<AttendanceRecordDto>.Create(items, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<AttendanceHistoryMonthDto> GetAttendanceMonthCalendarAsync(
        int employeeId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(employeeId);
        var dailyTargetHours = (double)employee.DailyWorkHours;

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var records = await _attendanceRepository.GetByDateRangeAsync(
            employeeId, monthStart, monthEnd, cancellationToken);
        var absences = await _absenceRepository.GetByEmployeeIdAndDateRangeAsync(
            employeeId, DateOnly.FromDateTime(monthStart), DateOnly.FromDateTime(monthEnd), cancellationToken);

        // Group records by calendar day (DateOnly) and pick the latest record per day.
        // This prevents ToDictionary from throwing when multiple records have the same calendar date.
        var recordLookup = records
            .GroupBy(r => DateOnly.FromDateTime(r.ClockInTime))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.ClockInTime).First());

        // Same rationale as recordLookup — guards against more than one absence report per day.
        var absenceLookup = absences
            .GroupBy(a => a.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).First());

        var firstDay = new DateOnly(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var today = DateOnly.FromDateTime(await _timeProvider.GetCurrentTimeAsync(cancellationToken));
        var monthClosed = false;

        static string FormatDuration(TimeSpan? duration) => duration is null ? "00:00" : duration.Value.ToString("hh\\:mm");
        // Signed HH:MM — "-01:00" (short of target) / "+00:30" (over target).
        static string FormatSignedDuration(TimeSpan diff)
        {
            var sign = diff < TimeSpan.Zero ? "-" : diff > TimeSpan.Zero ? "+" : "";
            return $"{sign}{diff.Duration():hh\\:mm}";
        }
        static string ToHebrewDayName(DayOfWeek dayOfWeek) => dayOfWeek switch
        {
            DayOfWeek.Sunday => "יום ראשון",
            DayOfWeek.Monday => "יום שני",
            DayOfWeek.Tuesday => "יום שלישי",
            DayOfWeek.Wednesday => "יום רביעי",
            DayOfWeek.Thursday => "יום חמישי",
            DayOfWeek.Friday => "יום שישי",
            _ => "שבת"
        };

        var days = new List<AttendanceHistoryDayDto>(daysInMonth);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = firstDay.AddDays(day - 1);
            var record = recordLookup.TryGetValue(date, out var matchedRecord) ? matchedRecord : null;
            var absence = absenceLookup.TryGetValue(date, out var matchedAbsence) ? matchedAbsence : null;
            var dayOfWeek = date.DayOfWeek.ToString();
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Friday;
            var isFutureDate = date > today;
            var isDisabled = isWeekend || isFutureDate || monthClosed;
            var isEditable = !isDisabled && !monthClosed;
            var canClockIn = isEditable && record is null;
            var canClockOut = isEditable && record is not null && record.ClockOutTime is null;
            var status = absence is not null
                ? "Absence"
                : record is null
                    ? (isWeekend ? "Weekend" : isFutureDate ? "Future" : "Empty")
                    : (record.ClockOutTime is null ? "Active" : "Completed");
            var rowType = absence is not null
                ? "Absence"
                : record is null
                    ? (isWeekend ? "Weekend" : isFutureDate ? "Future" : "Empty")
                    : "Attendance";
            var gap = record?.Duration is not null
                ? record.Duration.Value - TimeSpan.FromHours(dailyTargetHours)
                : (TimeSpan?)null;
            var hasDeficit = gap is not null && gap.Value < TimeSpan.Zero;
            var alertText = record is null
                ? (isWeekend ? "אין דרישה לנוכחות בסופ" + "\"" + "ש" : isFutureDate ? "היום עדיין לא התרחש" : null)
                : hasDeficit ? "חוסר" : null;
            var explanation = absence is not null
                ? "היעדרות מדווחת"
                : record is null
                    ? (isWeekend ? "סוף שבוע" : isFutureDate ? "יום עתידי" : "אין רשומה")
                    : (record.ClockOutTime is null ? "בעבודה" : "הושלם");

            days.Add(new AttendanceHistoryDayDto
            {
                Date = date,
                DayOfWeek = dayOfWeek,
                DisplayDateLabel = date.ToString("dd/MM/yyyy"),
                DayLabel = ToHebrewDayName(date.DayOfWeek),
                DayTypeLabel = isWeekend ? "סוף שבוע" : "רגיל",
                HasAttendanceRecord = record is not null,
                IsWeekend = isWeekend,
                IsFutureDate = isFutureDate,
                IsDisabled = isDisabled,
                IsEditable = isEditable,
                IsMonthClosed = monthClosed,
                CanClockIn = canClockIn,
                CanClockOut = canClockOut,
                Status = status,
                ClockInTime = record?.ClockInTime,
                ClockOutTime = record?.ClockOutTime,
                TotalWorkedHours = record?.Duration is not null ? FormatDuration(record.Duration) : string.Empty,
                RowType = rowType,
                HasAlert = !string.IsNullOrWhiteSpace(alertText) || hasDeficit,
                HasDeficit = hasDeficit,
                AlertText = alertText,
                DisplayBalance = gap is not null ? FormatSignedDuration(gap.Value) : null,
                Explanation = explanation,
                AbsenceType = absence?.Type.ToString(),
                DocumentUrl = absence?.DocumentUrl,
                Note = absence?.Note ?? record?.Note
            });
        }

        var totalWorked = records
            .Where(r => r.Duration is not null)
            .Select(r => r.Duration!.Value)
            .Aggregate(TimeSpan.Zero, (sum, next) => sum + next);
        var notesCount = records.Count(r => !string.IsNullOrWhiteSpace(r.Note))
                        + absences.Count(a => !string.IsNullOrWhiteSpace(a.Note));

        return new AttendanceHistoryMonthDto
        {
            Year = year,
            Month = month,
            Summary = new AttendanceHistoryMonthSummaryDto
            {
                TotalWorkedHours = FormatDuration(totalWorked),
                RegularHours = FormatDuration(totalWorked),
                DeficitHours = "00:00",
                BreakHours = "00:00",
                NotesCount = notesCount
            },
            Days = days
        };
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

    /// <inheritdoc />
    /// <exception cref="AttendanceRecordNotFoundException">Record ID does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Requesting employee does not own the record.</exception>
    public async Task<AttendanceRecordDto> ManualUpdateAsync(
        int requestingEmployeeId,
        ManualTimeUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _manualUpdateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var record = await _attendanceRepository.GetByIdAsync(request.RecordId, cancellationToken)
            ?? throw new AttendanceRecordNotFoundException(request.RecordId);

        //if (record.EmployeeId != requestingEmployeeId)
        //    throw new UnauthorizedAccessException(
        //        $"Employee {requestingEmployeeId} is not authorised to modify record {request.RecordId}.");

        record.ManualUpdate(request.NewClockInTime, request.NewClockOutTime, request.Note!);
        await _attendanceRepository.UpdateAsync(record, cancellationToken);

        _logger.LogInformation(
            "Manual attendance update. EmployeeId={EmployeeId} RecordId={RecordId} ClockIn={ClockIn} ClockOut={ClockOut}",
            requestingEmployeeId, record.Id, record.ClockInTime, record.ClockOutTime);

        return MapToDto(record);
    }

    /// <inheritdoc />
    /// <exception cref="AttendanceRecordNotFoundException">Record ID does not exist.</exception>
    public async Task<AttendanceRecordDto> AdminManualUpdateAsync(
        ManualTimeUpdateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _manualUpdateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var record = await _attendanceRepository.GetByIdAsync(request.RecordId, cancellationToken)
            ?? throw new AttendanceRecordNotFoundException(request.RecordId);

        record.ManualUpdate(request.NewClockInTime, request.NewClockOutTime, request.Note!);
        await _attendanceRepository.UpdateAsync(record, cancellationToken);

        _logger.LogInformation(
            "Admin manual attendance update. RecordId={RecordId} EmployeeId={EmployeeId} ClockIn={ClockIn} ClockOut={ClockOut}",
            record.Id, record.EmployeeId, record.ClockInTime, record.ClockOutTime);

        return MapToDto(record);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Employee ID not found.</exception>
    public async Task<AttendanceRecordDto> ManualAddShiftAsync(
        int targetEmployeeId,
        ManualAddShiftRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _manualAddShiftValidator.ValidateAndThrowAsync(request, cancellationToken);

        _ = await _employeeRepository.GetByIdAsync(targetEmployeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(targetEmployeeId);

        var now       = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var clockIn   = request.Date.ToDateTime(request.ClockInTime);
        var clockOut  = request.Date.ToDateTime(request.ClockOutTime);

        var record    = AttendanceRecord.CreateManual(targetEmployeeId, clockIn, clockOut, request.Note!, now);
        var persisted = await _attendanceRepository.AddAsync(record, cancellationToken);

        _logger.LogInformation(
            "Manual shift added. EmployeeId={EmployeeId} RecordId={RecordId} Date={Date} ClockIn={ClockIn} ClockOut={ClockOut}",
            targetEmployeeId, persisted.Id, request.Date, clockIn, clockOut);

        return MapToDto(persisted);
    }

    /// <inheritdoc />
    /// <exception cref="EmployeeNotFoundException">Resolved target employee does not exist.</exception>
    public async Task<AttendanceRecordDto> AdminManualAddShiftAsync(
        int fallbackEmployeeId,
        ManualAddShiftRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _manualAddShiftValidator.ValidateAndThrowAsync(request, cancellationToken);

        var targetEmployeeId = request.EmployeeId ?? fallbackEmployeeId;

        _ = await _employeeRepository.GetByIdAsync(targetEmployeeId, cancellationToken)
            ?? throw new EmployeeNotFoundException(targetEmployeeId);

        var now       = await _timeProvider.GetCurrentTimeAsync(cancellationToken);
        var clockIn   = request.Date.ToDateTime(request.ClockInTime);
        var clockOut  = request.Date.ToDateTime(request.ClockOutTime);

        var record    = AttendanceRecord.CreateManual(targetEmployeeId, clockIn, clockOut, request.Note!, now);
        var persisted = await _attendanceRepository.AddAsync(record, cancellationToken);

        _logger.LogInformation(
            "Admin manual shift added. TargetEmployeeId={TargetEmployeeId} RecordId={RecordId} Date={Date} ClockIn={ClockIn} ClockOut={ClockOut}",
            targetEmployeeId, persisted.Id, request.Date, clockIn, clockOut);

        return MapToDto(persisted);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static AttendanceRecordDto MapToDto(AttendanceRecord record) => new()
    {
        Id               = record.Id,
        EmployeeId       = record.EmployeeId,
        EmployeeFullName = record.Employee?.FullName ?? string.Empty,
        ClockInTime      = record.ClockInTime,
        ClockOutTime     = record.ClockOutTime,
        Duration         = record.Duration,
        CreatedAt        = record.CreatedAt,
        Note             = record.Note
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
