namespace TaskFlow.Api.Domain;

/// <summary>
/// Domain model representing a single task item.
/// This type is framework-agnostic and enforces basic invariants.
/// </summary>
public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Creates a new task item.
    /// Invariants are enforced here so an invalid TaskItem cannot be constructed.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when title is null/empty/whitespace.</exception>
    public TaskItem(string title, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Id = Guid.NewGuid();
        Title = title.Trim();
        Description = description;
        IsCompleted = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the task as completed. This operation is idempotent.
    /// </summary>
    public void MarkComplete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
    }
}
