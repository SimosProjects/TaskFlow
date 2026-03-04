using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Domain;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Infrastructure;

namespace TaskFlow.Api.Services;

/// <summary>
/// EF Core implementation of <see cref="ITaskService"/> backed by PostgreSQL.
/// Keeps controllers thin while centralizing data access + use-case logic.
/// </summary>
public sealed class TaskService : ITaskService
{
    private readonly TaskFlowDbContext _db;
    private readonly ILogger<TaskService> _logger;

    /// <summary>
    /// Constructs the service with the EF Core DbContext and logger.
    /// DbContext is Scoped (per-request) and not thread-safe, matching typical production patterns.
    /// </summary>
    public TaskService(TaskFlowDbContext db, ILogger<TaskService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a page of tasks from the database as read-only DTOs.
    /// Applies optional filters, enforces paging guardrails, and returns paging metadata.
    /// </summary>
    public async Task<PagedResult<TaskResponse>> GetAllAsync(TaskQueryParameters query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);
        var search = NormalizeSearch(query.Search);

        IQueryable<TaskItem> tasksQuery = _db.Tasks.AsNoTracking();

        if (query.IsCompleted is not null)
        {
            tasksQuery = tasksQuery.Where(t => t.IsCompleted == query.IsCompleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            // PostgreSQL-friendly, case-insensitive substring search.
            tasksQuery = tasksQuery.Where(t => EF.Functions.ILike(t.Title, $"%{search}%"));
        }

        // Count is computed on the filtered query (before paging) for accurate metadata.
        var totalCount = await tasksQuery.CountAsync(ct);

        var items = await tasksQuery
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsCompleted = t.IsCompleted,
                CreatedAtUtc = t.CreatedAtUtc
            })
            .ToListAsync(ct);

        return PagedResult<TaskResponse>.Create(items, page, pageSize, totalCount);
    }

    /// <summary>
    /// Retrieves a task by its identifier or returns null when not found.
    /// Uses AsNoTracking and server-side projection to avoid materializing a full entity.
    /// </summary>
    public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Tasks
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                IsCompleted = t.IsCompleted,
                CreatedAtUtc = t.CreatedAtUtc
            })
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Creates a new task and persists it to the database.
    /// Domain object construction enforces invariants at the boundary.
    /// Mapping delegated to TaskMapping.ToResponse() — the single authoritative mapping path.
    /// </summary>
    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem(request.Title, request.Description);

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Task created with Id {TaskId}", task.Id);

        return task.ToResponse();
    }

    /// <summary>
    /// Marks a task as completed and persists the change.
    /// Returns false when no matching task exists.
    /// </summary>
    public async Task<bool> CompleteAsync(Guid id, CancellationToken ct = default)
    {
        // Intentionally tracked query: we are modifying the entity and persisting changes.
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            _logger.LogWarning("Attempted to complete non-existent task {TaskId}", id);
            return false;
        }

        task.MarkComplete();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Task {TaskId} marked as completed.", id);
        return true;
    }

    /// <summary>
    /// Normalizes paging inputs to prevent unbounded queries and to keep API behavior predictable.
    /// </summary>
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        const int DefaultPage = 1;
        const int DefaultPageSize = 20;
        const int MaxPageSize = 100;

        var normalizedPage = page < 1 ? DefaultPage : page;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);

        return (normalizedPage, normalizedPageSize);
    }

    /// <summary>
    /// Normalizes search input to keep query behavior consistent and avoid accidental whitespace-only filters.
    /// </summary>
    private static string? NormalizeSearch(string? search)
    {
        var trimmed = search?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
