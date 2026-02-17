using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

/// <summary>
/// In-memory implementation of <see cref="ITaskService"/>.
/// 
/// This service encapsulates business logic related to task management.
/// It is intentionally registered as a Singleton because it maintains
/// in-memory state for the lifetime of the application.
/// 
/// </summary>
public class TaskService : ITaskService
{
    // Lock object guarding the in-memory task store.
    // ASP.NET Core processes requests concurrently, and because this
    // service is registered as a Singleton, shared state must be synchronized.
    private readonly object _gate = new();


    // In-memory storage for tasks.
    // Because this service is registered as a Singleton,
    // this list persists for the lifetime of the application.
    private readonly List<TaskItem> _tasks = new();

    /// <summary>
    /// Retrieves all tasks currently stored in memory.
    /// Returns a snapshot to avoid exposing internal mutable state.
    /// </summary>
    public IEnumerable<TaskItem> GetAll()
    {
        lock (_gate)
        {
            return _tasks.ToList();
        }
    }

    /// <summary>
    /// Retrieves a task by its unique identifier.
    /// Returns null when no matching task exists.
    /// </summary>
    public TaskItem? GetById(Guid id)
    {
        lock (_gate)
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    /// <summary>
    /// Creates a new task and stores it in memory.
    /// Domain object construction enforces invariants.
    /// </summary>
    public TaskItem Create(string title, string? description)
    {
        var task = new TaskItem(title, description);

        lock (_gate)
        {
            // Business decision: tasks are stored immediately upon creation.
            _tasks.Add(task);
        }

        return task;
    }

    /// <summary>
    /// Marks a task as completed.
    /// Returns false when the task does not exist.
    /// </summary>
    public bool Complete(Guid id)
    {
        lock (_gate)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task is null)
                return false;

            // Business behavior is expressed through the domain model.
            task.MarkComplete();

            return true;
        }
    }
}
