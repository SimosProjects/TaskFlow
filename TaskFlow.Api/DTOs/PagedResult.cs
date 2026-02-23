namespace TaskFlow.Api.DTOs;

/// <summary>
/// Standard paged response envelope for list endpoints.
/// Provides paging metadata alongside the returned items.
/// </summary>
public sealed class PagedResult<T>
{
    /// <summary>
    /// Items for the requested page.
    /// </summary>
    public required IReadOnlyList<T> Items { get; init; }

    /// <summary>
    /// 1-based page number actually applied by the API.
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Page size actually applied by the API (after caps/defaults).
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of items matching the filters (ignores paging).
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Indicates whether another page exists after the current one.
    /// </summary>
    public required bool HasNext { get; init; }

    /// <summary>
    /// Creates a paged result envelope.
    /// </summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        // totalCount may be larger than items.Count due to paging; compute whether more items exist after this page.
        var hasNext = totalCount > (page * pageSize);

        return new PagedResult<T>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            HasNext = hasNext
        };
    }
}