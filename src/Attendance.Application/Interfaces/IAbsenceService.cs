using Attendance.Application.DTOs;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the business operations for reporting employee absences.
/// </summary>
public interface IAbsenceService
{
    /// <summary>
    /// Reports a new absence for the specified employee.
    /// </summary>
    /// <param name="employeeId">The ID of the employee the absence belongs to.</param>
    /// <param name="request">Absence date, type, optional document URL, and optional note.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created <see cref="AbsenceRecordDto"/>.</returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">No employee with <paramref name="employeeId"/> exists.</exception>
    /// <exception cref="FluentValidation.ValidationException">
    /// Thrown when <paramref name="request"/> is invalid (e.g. missing required document).
    /// </exception>
    Task<AbsenceRecordDto> ReportAbsenceAsync(
        int employeeId,
        ReportAbsenceRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the employee ID that owns the absence report referencing the given document URL.
    /// Used to authorize document downloads.
    /// </summary>
    /// <param name="documentUrl">The exact document URL to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The owning employee's ID, or <c>null</c> if no absence report references this document.</returns>
    Task<int?> GetOwningEmployeeIdForDocumentAsync(string documentUrl, CancellationToken cancellationToken = default);
}
