using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Attendance.Application.Constants;
using Attendance.Application.DTOs;
using Attendance.Application.Exceptions;
using Attendance.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Attendance.Api.Controllers;

/// <summary>
/// Provides clock-in/out and attendance history endpoints for the authenticated employee.
/// UserId is extracted from the JWT — employees can only access their own records.
/// </summary>
[ApiController]
[Route("api/attendance")]
[Authorize(Roles = "Employee,Admin")]
[Produces("application/json")]
public sealed class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAbsenceService _absenceService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<AttendanceController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceController"/>.
    /// </summary>
    public AttendanceController(
        IAttendanceService attendanceService,
        IAbsenceService absenceService,
        IFileStorageService fileStorageService,
        ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _absenceService = absenceService;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Records a clock-in event for the authenticated employee.
    /// The timestamp is sourced from the external time provider (Asia/Jerusalem).
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly created attendance record.</returns>
    [HttpPost("clock-in")]
    [SwaggerOperation(
        Summary = "Clock in",
        Description = "Opens a new attendance session using the external time provider. Fails if a session is already open.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Clocked in successfully.",         typeof(AttendanceRecordDto))]
    [SwaggerResponse(StatusCodes.Status409Conflict,     "Employee is already clocked in.",  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClockIn(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.ClockInAsync(employeeId, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/clock-in succeeded. EmployeeId={EmployeeId} RecordId={RecordId}",
            employeeId, result.Id);

        return Ok(result);
    }

    /// <summary>
    /// Records a clock-out event for the authenticated employee's active session.
    /// The timestamp is sourced from the external time provider (Asia/Jerusalem).
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated attendance record with clock-out time and duration.</returns>
    [HttpPost("clock-out")]
    [SwaggerOperation(
        Summary = "Clock out",
        Description = "Closes the active attendance session using the external time provider. Fails if no session is open.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Clocked out successfully.",        typeof(AttendanceRecordDto))]
    [SwaggerResponse(StatusCodes.Status409Conflict,     "No active session to close.",      typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClockOut(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.ClockOutAsync(employeeId, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/clock-out succeeded. EmployeeId={EmployeeId} Duration={Duration}",
            employeeId, result.Duration);

        return Ok(result);
    }

    /// <summary>
    /// Returns the authenticated employee's current attendance status,
    /// including elapsed time if they are clocked in.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Real-time status with optional active session details.</returns>
    [HttpGet("status")]
    [SwaggerOperation(
        Summary = "Get current status",
        Description = "Returns whether the employee is clocked in and, if so, the active record details and elapsed duration.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Status retrieved successfully.",   typeof(CurrentAttendanceStatusDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(CurrentAttendanceStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),             StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.GetCurrentStatusAsync(employeeId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated attendance history for the authenticated employee,
    /// ordered by clock-in time descending.
    /// </summary>
    /// <param name="pageNumber">1-based page number (default: 1).</param>
    /// <param name="pageSize">Records per page, max 100 (default: 20).</param>
    /// <param name="year">Optional year filter for the history query.</param>
    /// <param name="month">Optional month filter for the history query.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Paginated list of attendance records with total count and page metadata.</returns>
    [HttpGet("history")]
    [SwaggerOperation(
        Summary = "Get attendance history",
        Description = "Returns a server-side paginated list of the employee's attendance records, most recent first.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "History retrieved successfully.",  typeof(PagedResult<AttendanceRecordDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Invalid pagination parameters.",   typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(PagedResult<AttendanceRecordDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),                   StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),                   StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory(
        [FromQuery][Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery][Range(1, 100)]          int pageSize   = 20,
        [FromQuery] int? year = null,
        [FromQuery][Range(1, 12)] int? month = null,
        CancellationToken cancellationToken = default)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.GetAttendanceHistoryAsync(
            employeeId, pageNumber, pageSize, year, month, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a complete calendar view for the selected month, including every day and any matching attendance data.
    /// </summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month.</param>
    /// <param name="employeeId">Optional employee id to view; only allowed for Admin users. If not provided, returns the authenticated employee's calendar.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Full month calendar view with one row per day.</returns>
    [HttpGet("history/calendar")]
    [SwaggerOperation(
        Summary = "Get month calendar history",
        Description = "Returns one row per calendar day for the selected month, merged with any attendance data.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Calendar history retrieved successfully.", typeof(AttendanceHistoryMonthDto))]
    [ProducesResponseType(typeof(AttendanceHistoryMonthDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryCalendar(
        [FromQuery][Range(2000, 2100)] int year,
        [FromQuery][Range(1, 12)] int month,
        [FromQuery] int? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        if (employeeId.HasValue && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var targetEmployeeId = employeeId ?? GetCurrentUserId();
        var result = await _attendanceService.GetAttendanceMonthCalendarAsync(targetEmployeeId, year, month, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Calculates the total hours worked during the current ISO week (Monday–Sunday)
    /// for the authenticated employee.
    /// Active sessions contribute their elapsed time up to the current moment.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Total weekly worked hours in multiple formats.</returns>
    [HttpGet("weekly-hours")]
    [SwaggerOperation(
        Summary = "Get weekly hours",
        Description = "Returns total hours worked in the current ISO week (Monday 00:00 – Sunday 23:59). Active sessions count up to the current moment.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Weekly hours calculated.",         typeof(WorkedHoursDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(WorkedHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWeeklyHours(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var duration = await _attendanceService.GetWeeklyHoursAsync(employeeId, cancellationToken);
        return Ok(WorkedHoursDto.FromTimeSpan(duration));
    }

    /// <summary>
    /// Calculates the total hours worked during the current calendar month
    /// for the authenticated employee.
    /// Active sessions contribute their elapsed time up to the current moment.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>Total monthly worked hours in multiple formats.</returns>
    [HttpGet("monthly-hours")]
    [SwaggerOperation(
        Summary = "Get monthly hours",
        Description = "Returns total hours worked in the current calendar month. Active sessions count up to the current moment.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Monthly hours calculated.",        typeof(WorkedHoursDto))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(WorkedHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMonthlyHours(CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var duration = await _attendanceService.GetMonthlyHoursAsync(employeeId, cancellationToken);
        return Ok(WorkedHoursDto.FromTimeSpan(duration));
    }

    /// <summary>
    /// Retroactively adjusts an attendance record's clock-in and clock-out times.
    /// A mandatory reason note must be provided — this rule only applies to manual updates,
    /// not to regular clock-in/out operations.
    /// Employees may only modify their own records.
    /// </summary>
    /// <param name="request">Update payload: record ID, new times, and reason note.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The updated attendance record.</returns>
    [HttpPut("manual-update")]
    [SwaggerOperation(
        Summary = "Manual attendance update",
        Description = "Retroactively adjusts clock-in/out times for an existing record. " +
                      "The Note field is REQUIRED for this endpoint. " +
                      "Employees may only update their own records.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "Record updated successfully.",      typeof(AttendanceRecordDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Validation failed (e.g. missing note, invalid time range).", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",               typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Attempting to modify another employee's record.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound,     "Attendance record not found.",     typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ManualUpdate(
        [FromBody] ManualTimeUpdateRequestDto request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.ManualUpdateAsync(employeeId, request, cancellationToken);

        _logger.LogInformation(
            "PUT /api/attendance/manual-update succeeded. EmployeeId={EmployeeId} RecordId={RecordId}",
            employeeId, result.Id);

        return Ok(result);
    }

    /// <summary>
    /// Creates a new historical attendance record for the authenticated employee.
    /// Use this to back-fill a missed or unrecorded past shift.
    /// A mandatory reason note must be provided for the audit trail.
    /// </summary>
    /// <param name="request">Date, clock-in time, clock-out time, and reason note for the new record.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly created attendance record.</returns>
    [HttpPost("manual-add")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Add manual shift",
        Description = "Creates a new completed attendance record for a past shift. " +
                      "The Note field is REQUIRED. The record is created for the authenticated employee.")]
    [SwaggerResponse(StatusCodes.Status201Created,        "Record created successfully.",      typeof(AttendanceRecordDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,     "Validation failed (e.g. missing note, clock-out before clock-in).", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized,   "Not authenticated.",               typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AttendanceRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),      StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ManualAddShift(
        [FromBody] ManualAddShiftRequestDto request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _attendanceService.ManualAddShiftAsync(employeeId, request, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/manual-add succeeded. EmployeeId={EmployeeId} RecordId={RecordId}",
            employeeId, result.Id);

        return CreatedAtAction(nameof(GetHistory), result);
    }

    /// <summary>
    /// Uploads a supporting document (e.g. a doctor's note) for an absence report.
    /// Accepts PDF, JPG, JPEG, and PNG files up to 10 MB. The returned URL is passed as
    /// <c>DocumentUrl</c> to <see cref="ReportAbsence"/>.
    /// </summary>
    /// <param name="file">The document file to upload.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The generated URL/path where the file was stored.</returns>
    [HttpPost("upload-document")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DocumentUploadPolicy.MaxFileSizeBytes)]
    [SwaggerOperation(
        Summary = "Upload absence supporting document",
        Description = "Saves an uploaded document (PDF/JPG/JPEG/PNG, max 10MB) and returns its generated URL.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "File uploaded successfully.",                typeof(UploadDocumentResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "No file provided or file type not allowed.", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",                          typeof(ProblemDetails))]
    [ProducesResponseType(typeof(UploadDocumentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails),            StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),            StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
   {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "No File Provided",
                Detail = "A file must be attached to the request."
            });
        }

        var employeeId = GetCurrentUserId();

        await using var stream = file.OpenReadStream();
        var url = await _fileStorageService.SaveFileAsync(stream, file.FileName, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/upload-document succeeded. EmployeeId={EmployeeId} FileName={FileName}",
            employeeId, file.FileName);

        return Ok(new UploadDocumentResponseDto { Url = url, FileName = file.FileName });
    }

    /// <summary>
    /// Reports an absence (vacation, sick leave, holiday, etc.) for the authenticated employee.
    /// </summary>
    /// <param name="request">Absence date, type, optional supporting document URL, and optional note.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The newly created absence record.</returns>
    [HttpPost("report-absence")]
    [Consumes("application/json")]
    [SwaggerOperation(
        Summary = "Report absence",
        Description = "Creates a new absence report. A DocumentUrl is required for SickLeave, ChildSickLeave, and Other.")]
    [SwaggerResponse(StatusCodes.Status201Created,      "Absence reported successfully.",                       typeof(AbsenceRecordDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest,   "Validation failed (e.g. missing required document).", typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",                                   typeof(ProblemDetails))]
    [ProducesResponseType(typeof(AbsenceRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails),   StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails),   StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReportAbsence(
        [FromBody] ReportAbsenceRequestDto request,
        CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();
        var result = await _absenceService.ReportAbsenceAsync(employeeId, request, cancellationToken);

        _logger.LogInformation(
            "POST /api/attendance/report-absence succeeded. EmployeeId={EmployeeId} AbsenceId={AbsenceId} Type={Type}",
            employeeId, result.Id, result.AbsenceType);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Downloads a previously uploaded absence supporting document.
    /// Employees may only download documents linked to their own absence reports;
    /// admins may download any document. A document not yet linked to any absence
    /// report (i.e. uploaded but not yet submitted via <see cref="ReportAbsence"/>)
    /// is not downloadable by non-admins.
    /// </summary>
    /// <param name="fileName">The generated file name from the upload response's URL.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    /// <returns>The file content stream.</returns>
    [HttpGet("documents/{fileName}")]
    [SwaggerOperation(
        Summary = "Download absence supporting document",
        Description = "Streams a previously uploaded document. Employees may only access documents linked to their own absence reports; admins may access any.")]
    [SwaggerResponse(StatusCodes.Status200OK,           "File streamed successfully.")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated.",                                  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status403Forbidden,    "Attempting to access another employee's document.",  typeof(ProblemDetails))]
    [SwaggerResponse(StatusCodes.Status404NotFound,     "File not found.",                                     typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(string fileName, CancellationToken cancellationToken)
    {
        var employeeId = GetCurrentUserId();

        if (!User.IsInRole("Admin"))
        {
            var documentUrl = $"/api/attendance/documents/{fileName}";
            var owningEmployeeId = await _absenceService.GetOwningEmployeeIdForDocumentAsync(documentUrl, cancellationToken);

            if (owningEmployeeId is null || owningEmployeeId.Value != employeeId)
                return Forbid();
        }

        var stream = await _fileStorageService.OpenReadAsync(fileName, cancellationToken);
        if (stream is null)
            return NotFound();

        return File(stream, GetContentType(fileName), fileName);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Maps a document file extension to its MIME content type.
    /// Only ever called with names already validated against <c>DocumentUploadPolicy.AllowedExtensions</c>.
    /// </summary>
    private static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };

    /// <summary>
    /// Extracts and parses the employee ID from the <c>sub</c> JWT claim
    /// (mapped to <see cref="ClaimTypes.NameIdentifier"/> by the JWT middleware).
    /// </summary>
    /// <exception cref="AuthenticationException">Claim is missing or not a valid integer.</exception>
    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !int.TryParse(value, out var userId))
        {
            throw new AuthenticationException(
                "The authenticated token does not contain a valid user identifier.");
        }

        return userId;
    }
}
