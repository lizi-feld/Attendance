using Attendance.Domain.Entities;

namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the data access contract for <see cref="Employee"/> aggregate operations.
/// Implementations live in the Infrastructure layer and are injected via DI.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// Retrieves an employee by their unique database identifier.
    /// </summary>
    /// <param name="id">The employee's primary key.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>The matching <see cref="Employee"/>, or <c>null</c> if not found.</returns>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an employee by their username (case-insensitive lookup).
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>The matching <see cref="Employee"/>, or <c>null</c> if not found.</returns>
    Task<Employee?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all registered employees ordered by full name ascending.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A read-only list of all <see cref="Employee"/> records.</returns>
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single page of employees ordered by full name ascending, plus the total count.
    /// Used for paginated admin listing.
    /// </summary>
    /// <param name="pageNumber">1-based page index.</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns>A tuple of the page items and total employee count.</returns>
    Task<(IReadOnlyList<Employee> Employees, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a new employee entity and returns it with its generated primary key populated.
    /// </summary>
    /// <param name="employee">The employee entity to insert.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    /// <returns>The persisted <see cref="Employee"/> with <c>Id</c> assigned by the database.</returns>
    Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists mutations on an existing employee entity.
    /// </summary>
    /// <param name="employee">The employee entity carrying the updated state.</param>
    /// <param name="cancellationToken">Token to cancel the database operation.</param>
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the given username is already registered (case-insensitive).
    /// </summary>
    /// <param name="username">The username to check for existence.</param>
    /// <param name="cancellationToken">Token to cancel the database query.</param>
    /// <returns><c>true</c> if the username is taken; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(string username, CancellationToken cancellationToken = default);
}
