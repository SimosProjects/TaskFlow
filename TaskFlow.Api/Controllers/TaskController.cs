using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    /// <summary>
    /// Initializes the controller with the task service abstraction.
    /// Constructor injection keeps dependencies explicit and supports unit testing.
    /// </summary>
    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Retrieves all tasks.
    /// This endpoint is intentionally thin; business logic and data access belong in the service layer.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponse>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TaskResponse>> GetAll()
        => Ok(_taskService.GetAll().Select(TaskMapping.ToResponse));

    /// <summary>
    /// Retrieves a single task by its identifier.
    /// Returns 404 when the task does not exist to preserve clear REST semantics.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<TaskResponse> GetById(Guid id)
    {
        var task = _taskService.GetById(id);
        return task is null ? NotFound() : Ok(TaskMapping.ToResponse(task));
    }

    /// <summary>
    /// Creates a new task using the request DTO as the API boundary.
    /// Model validation is enforced by [ApiController] + DataAnnotations (400 is returned automatically on invalid input).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TaskResponse> Create([FromBody] CreateTaskRequest request)
    {
        // API contract is validated at the boundary; domain logic stays in the service layer.
        var task = _taskService.Create(request.Title, request.Description);

        return CreatedAtAction(
            nameof(GetById),
            new { id = task.Id },
            TaskMapping.ToResponse(task));
    }

    /// <summary>
    /// Marks a task as completed.
    /// Returns 204 on success (no response body) and 404 if the task does not exist.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Complete(Guid id)
        => _taskService.Complete(id) ? NoContent() : NotFound();
}