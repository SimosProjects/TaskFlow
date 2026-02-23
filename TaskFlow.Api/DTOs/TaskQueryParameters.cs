namespace TaskFlow.Api.DTOs;

/// <summary>
/// Query parameters for retrieving tasks.
/// Centralizes paging and filtering inputs to keep controller/service signatures stable as the API evolves.
/// </summary>
public sealed class TaskQueryParameters
{
    /// <summary>
    /// 1-based page number. Values less than 1 are treated as page 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page. Values less than 1 use the default; values above the max are capped.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Optional completion filter. When null, both completed and incomplete tasks are returned.
    /// </summary>
    public bool? IsCompleted { get; init; }

    /// <summary>
    /// Optional search string applied to task titles.
    /// </summary>
    public string? Search { get; init; }
}