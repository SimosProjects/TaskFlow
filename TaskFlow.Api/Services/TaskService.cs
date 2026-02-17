using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

/// <summary>
/// In-memory implementation of <see cref="ITaskService"/>.
/// 
/// This service encapsulates task-related use cases.
/// It is registered as Scoped to match typical production patterns and to
/// prepare for EF Core integration (DbContext is Scoped and not thread-safe).
/// 
/// Note: with an in-memory list, Scoped lifetime means tasks will not persist
/// across separate HTTP requests. This is expected until a database is added.
/// </summary>
public class TaskService : ITaskService
{
    // In-memory storage for tasks.
    // With a Scoped lifetime, this list exists only for the duration of a single request.
    private readonly List<TaskItem> _tasks = new();

    private readonly ILogger<TaskService> _logger;

    /// <summary>
    /// Constructs the TaskService with required dependencies.
    /// 
    /// ILogger is injected via dependency injection to enable structured,
    /// centralized logging without coupling the service to a specific
    /// logging implementation.
    /// 
    /// ASP.NET Core provides ILogger&lt;T&gt; as a built-in DI service.
    /// </summary>
    public TaskService(ILogger<TaskService> logger)
    {
        _logger = logger;

        _logger.LogDebug("TaskService instance created: {InstanceId}", GetHashCode());
    }

    /// <summary>
    /// Retrieves all tasks currently stored in memory.
    /// Returns a snapshot to avoid exposing internal mutable state.
    /// </summary>
    public IEnumerable<TaskItem> GetAll() => _tasks.ToList();

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// Returns null when no matching task exists.
    /// </summary>
    public TaskItem? GetById(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Creates a new task and stores it in memory.
    /// Domain object construction enforces invariants.
    /// </summary>
    public TaskItem Create(string title, string? description)
    {
        var task = new TaskItem(title, description);

        _tasks.Add(task);

        _logger.LogInformation("Task created with Id {TaskId}", task.Id);

        return task;
    }

    /// <summary>
    /// Marks a task as completed.
    /// Returns false when the task does not exist.
    /// </summary>
    public bool Complete(Guid id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task is null)
        {
            _logger.LogWarning("Attempted to complete non-existent task {TaskId}", id);
            return false;
        }

        task.MarkComplete();

        _logger.LogInformation("Task {TaskId} marked as completed.", id);

        return true;
    }
}