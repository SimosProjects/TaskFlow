using TaskFlow.Api.DTOs;

namespace TaskFlow.Api.Services;

/// <summary>
/// Application service for task-related use cases.
/// Controllers call this abstraction to keep HTTP concerns separate from business logic.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Retrieves a snapshot of all tasks.
    /// </summary>
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// Returns null when no matching task exists.
    /// </summary>
    Task<TaskResponse?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new task with the provided title and optional description.
    /// </summary>
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);

    /// <summary>
    /// Marks a task as completed.
    /// Returns false when the task does not exist.
    /// </summary>
    Task<bool> CompleteAsync(Guid id, CancellationToken ct = default);
}