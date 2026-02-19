using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Api.Services;
using Xunit;

namespace TaskFlow.Tests.Services;

/// <summary>
/// Unit tests for TaskService.
/// 
/// These tests validate application-layer use cases without involving
/// ASP.NET, middleware, or database infrastructure.
/// </summary>
public class TaskServiceTests
{
    /// <summary>
    /// Creates a TaskService instance using a NullLogger to avoid
    /// coupling tests to logging infrastructure.
    /// </summary>
    private static TaskService CreateService()
        => new TaskService(NullLogger<TaskService>.Instance);

    /// <summary>
    /// Verifies that creating a task stores it and allows retrieval by ID.
    /// Ensures the service correctly orchestrates domain construction.
    /// </summary>
    [Fact]
    public void Create_Adds_Task_And_Can_Be_Retrieved()
    {
        var service = CreateService();

        var created = service.Create("Title", "Desc");

        var fetched = service.GetById(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Title", fetched.Title);
        Assert.Equal("Desc", fetched.Description);
        Assert.False(fetched.IsCompleted);
    }

    /// <summary>
    /// Ensures GetAll returns all tasks created during the service lifetime.
    /// </summary>
    [Fact]
    public void GetAll_Returns_All_Created_Tasks()
    {
        var service = CreateService();

        service.Create("T1", null);
        service.Create("T2", "D2");

        var all = service.GetAll().ToList();

        Assert.Equal(2, all.Count);
    }

    /// <summary>
    /// Verifies that Complete returns false when the task does not exist.
    /// This defines the service contract for missing resources.
    /// </summary>
    [Fact]
    public void Complete_Returns_False_When_Task_Does_Not_Exist()
    {
        var service = CreateService();

        var result = service.Complete(Guid.NewGuid());

        Assert.False(result);
    }

    /// <summary>
    /// Ensures Complete transitions an existing task into a completed state.
    /// </summary>
    [Fact]
    public void Complete_Marks_Task_As_Completed_When_It_Exists()
    {
        var service = CreateService();

        var task = service.Create("Title", null);

        var result = service.Complete(task.Id);

        Assert.True(result);

        var fetched = service.GetById(task.Id);
        Assert.NotNull(fetched);
        Assert.True(fetched!.IsCompleted);
    }
}