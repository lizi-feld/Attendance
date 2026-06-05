namespace Attendance.Application.DTOs;

/// <summary>
/// A generic paginated result wrapper for list endpoints that support server-side paging.
/// Page numbers are 1-based.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>Gets the items on the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Gets the current 1-based page number.</summary>
    public int PageNumber { get; init; }

    /// <summary>Gets the maximum number of items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>Gets the total number of pages given the current <see cref="PageSize"/>.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Gets whether there is a page before the current one.</summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>Gets whether there is a page after the current one.</summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Creates a <see cref="PagedResult{T}"/> from an already-fetched page and its total count.
    /// </summary>
    /// <param name="items">The items on this page (already sliced by the data layer).</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <param name="pageNumber">The 1-based current page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        int totalCount,
        int pageNumber,
        int pageSize) =>
        new()
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
}
