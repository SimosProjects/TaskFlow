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
    /// Retrieves all tasks from the database as read-only DTOs.
    /// Uses AsNoTracking to avoid change-tracking overhead for read operations.
    /// </summary>
    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tasks = await _db.Tasks
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);

        return tasks.Select(MapToResponse).ToList();
    }

    /// <summary>
    /// Retrieves a task by its identifier or returns null when not found.
    /// Uses AsNoTracking since this operation does not modify the entity.
    /// </summary>
    public async Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var task = await _db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        return task is null ? null : MapToResponse(task);
    }

    /// <summary>
    /// Creates a new task and persists it to the database.
    /// Domain object construction enforces invariants at the boundary.
    /// </summary>
    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var task = new TaskItem(request.Title, request.Description);

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Task created with Id {TaskId}", task.Id);

        return MapToResponse(task);
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
    /// Maps a domain <see cref="TaskItem"/> entity to a <see cref="TaskResponse"/> DTO.
    /// This maintains a strict boundary between domain models and API response contracts.
    /// </summary>
    private static TaskResponse MapToResponse(TaskItem task) =>
        new TaskResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            IsCompleted = task.IsCompleted,
            CreatedAtUtc = task.CreatedAtUtc
        };
}