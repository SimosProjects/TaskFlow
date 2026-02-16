using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

public class TaskService : ITaskService
{
    private readonly List<TaskItem> _tasks = new();

    public IEnumerable<TaskItem> GetAll() => _tasks;

    public TaskItem? GetById(Guid id) => _tasks.FirstOrDefault(t => t.Id == id);

    public TaskItem Create(string title, string? description)
    {
        var task = new TaskItem(title, description);
        _tasks.Add(task);
        return task;
    }

    public bool Complete(Guid id)
    {
        var task = GetById(id);
        if (task is null) return false;

        task.MarkComplete();
        return true;
    }
}
