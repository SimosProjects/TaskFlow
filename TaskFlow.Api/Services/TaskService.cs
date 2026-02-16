using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

/// <summary>
/// In-memory implementation of <see cref="ITaskService"/>.
/// 
/// This service encapsulates business logic related to task management.
/// It is intentionally registered as a Singleton because it maintains
/// in-memory state for the lifetime of the application.
/// 
/// In a production system, this would likely be backed by a database
/// and registered with a Scoped lifetime.
/// </summary>
public class TaskService : ITaskService
{
    // In-memory storage for tasks.
    // Because this service is registered as a Singleton,
    // this list persists for the lifetime of the application.
    private readonly List<TaskItem> _tasks = new();

    /// <summary>
    /// Retrieves all tasks currently stored in memory.
    /// No filtering or paging is applied at this stage.
    /// </summary>
    public IEnumerable<TaskItem> GetAll() => _tasks;

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// Returns null when no matching task exists.
    /// </summary>
    public TaskItem? GetById(Guid id)
        => _tasks.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Creates a new task and stores it in memory.
    /// Domain object construction enforces invariants.
    /// </summary>
    public TaskItem Create(string title, string? description)
    {
        var task = new TaskItem(title, description);

        // Business decision: tasks are stored immediately upon creation.
        _tasks.Add(task);

        return task;
    }

    /// <summary>
    /// Marks a task as completed.
    /// Returns false when the task does not exist.
    /// </summary>
    public bool Complete(Guid id)
    {
        var task = GetById(id);
        if (task is null)
            return false;

        // Business behavior is expressed through the domain model.
        task.MarkComplete();

        return true;
    }
}
