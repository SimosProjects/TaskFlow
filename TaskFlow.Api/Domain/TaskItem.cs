namespace TaskFlow.Api.Domain;

public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public TaskItem(string title, string? description = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        IsCompleted = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkComplete() => IsCompleted = true;
}
