namespace SquidStd.Database.Abstractions.Data;

/// <summary>
/// A paginated result set with paging metadata.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResultData<T>
{
    /// <summary>Gets the items on the current page.</summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>Gets the 1-based page number.</summary>
    public required int Page { get; init; }

    /// <summary>Gets the page size.</summary>
    public required int PageSize { get; init; }

    /// <summary>Gets the total number of matching rows.</summary>
    public required long TotalCount { get; init; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)((TotalCount + PageSize - 1) / PageSize);

    /// <summary>Gets a value indicating whether a next page exists.</summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>Gets a value indicating whether a previous page exists.</summary>
    public bool HasPrevious => Page > 1 && TotalPages > 0;

    /// <summary>
    /// Creates a paged result.
    /// </summary>
    /// <param name="items">The current page items.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="totalCount">The total matching row count.</param>
    /// <returns>The paged result.</returns>
    public static PagedResultData<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalCount)
    {
        return new PagedResultData<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
