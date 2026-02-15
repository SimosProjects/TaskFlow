using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    [HttpGet("ping")]
    public ActionResult<object> Ping()
        => Ok(new { status = "ok", service = "TaskFlow.Api" });
}
