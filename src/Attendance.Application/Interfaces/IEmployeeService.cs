namespace Attendance.Application.Interfaces;

using Attendance.Application.Commands;
using Attendance.Application.DTOs;

/// <summary>
/// Defines employee management operations used by the admin API surface.
/// All methods return mapped DTOs — domain entities are never exposed to the API layer.
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Creates a new employee account.
    /// </summary>
    /// <param name="command">Validated creation command containing username, password, full name, and role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The newly created employee as an <see cref="EmployeeDto"/>.</returns>
    /// <exception cref="Exceptions.UsernameAlreadyExistsException">Thrown when the username is already taken.</exception>
    Task<EmployeeDto> CreateAsync(CreateEmployeeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates mutable fields on an existing employee. Only non-null properties in the command are applied.
    /// </summary>
    /// <param name="command">Validated update command; null fields are left unchanged.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated employee as an <see cref="EmployeeDto"/>.</returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    /// <exception cref="Exceptions.UsernameAlreadyExistsException">Thrown when the requested new username is already taken.</exception>
    Task<EmployeeDto> UpdateAsync(UpdateEmployeeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated, name-sorted list of all employees.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number.</param>
    /// <param name="pageSize">The maximum number of records per page.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="PagedResult{T}"/> of lightweight <see cref="EmployeeDto"/> entries.</returns>
    Task<PagedResult<EmployeeDto>> GetAllPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the full profile for a single employee, including their complete attendance history.
    /// </summary>
    /// <param name="id">The employee's unique database identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>An <see cref="EmployeeDetailsDto"/> with embedded attendance records.</returns>
    /// <exception cref="Exceptions.EmployeeNotFoundException">Thrown when no employee with the given ID exists.</exception>
    Task<EmployeeDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
