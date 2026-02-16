using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Domain;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TaskItem>> GetAll()
        => Ok(_taskService.GetAll());

    [HttpGet("{id:guid}")]
    public ActionResult<TaskItem> GetById(Guid id)
    {
        var task = _taskService.GetById(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    public ActionResult<TaskItem> Create([FromBody] CreateTaskRequest request)
    {
        var task = _taskService.Create(request.Title, request.Description);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPost("{id:guid}/complete")]
    public IActionResult Complete(Guid id)
        => _taskService.Complete(id) ? NoContent() : NotFound();
}

public record CreateTaskRequest(string Title, string? Description);
