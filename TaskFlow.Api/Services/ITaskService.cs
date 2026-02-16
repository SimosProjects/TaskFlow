using TaskFlow.Api.Domain;

namespace TaskFlow.Api.Services;

public interface ITaskService
{
    IEnumerable<TaskItem> GetAll();
    TaskItem? GetById(Guid id);
    TaskItem Create(string title, string? description);
    bool Complete(Guid id);
}
