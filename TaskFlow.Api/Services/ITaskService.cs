using TaskFlow.Api.Domain;

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
    IEnumerable<TaskItem> GetAll();

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// Returns null when no matching task exists.
    /// </summary>
    TaskItem? GetById(Guid id);

    /// <summary>
    /// Creates a new task with the provided title and optional description.
    /// </summary>
    TaskItem Create(string title, string? description);

    /// <summary>
    /// Marks a task as completed.
    /// Returns false when the task does not exist.
    /// </summary>
    bool Complete(Guid id);
}
