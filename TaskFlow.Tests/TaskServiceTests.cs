using TaskFlow.Api.Services;
using Xunit;

namespace TaskFlow.Tests;

/// <summary>
/// Unit tests for TaskService.
/// 
/// These tests validate business behavior in isolation,
/// without HTTP or ASP.NET Core infrastructure.
/// 
/// The goal is to ensure service-layer logic is correct
/// and independently testable.
/// </summary>
public class TaskServiceTests
{
    /// <summary>
    /// Verifies that creating a task:
    /// - Returns a non-null task
    /// - Preserves the provided title
    /// - Defaults IsCompleted to false
    /// - Generates a non-empty Guid
    /// 
    /// This ensures the domain model is constructed correctly
    /// and initial invariants are respected.
    /// </summary>
    [Fact]
    public void Create_AddsTask_AndReturnsIt()
    {
        var service = new TaskService();

        var task = service.Create("Test title", "Test description");

        Assert.NotNull(task);
        Assert.Equal("Test title", task.Title);
        Assert.False(task.IsCompleted);
        Assert.NotEqual(Guid.Empty, task.Id);
    }

    /// <summary>
    /// Verifies that calling Complete on an existing task:
    /// - Returns true
    /// - Marks the task as completed
    /// 
    /// This confirms that business behavior is expressed
    /// through the domain model and persisted in memory.
    /// </summary>
    [Fact]
    public void Complete_WhenTaskExists_MarksCompleted()
    {
        var service = new TaskService();
        var task = service.Create("Title", null);

        var result = service.Complete(task.Id);

        Assert.True(result);
        Assert.True(service.GetById(task.Id)!.IsCompleted);
    }

    /// <summary>
    /// Verifies that attempting to complete a non-existent task:
    /// - Returns false
    /// - Does not throw an exception
    /// 
    /// This ensures the service handles missing data gracefully
    /// and preserves predictable API semantics.
    /// </summary>
    [Fact]
    public void Complete_WhenTaskMissing_ReturnsFalse()
    {
        var service = new TaskService();

        var result = service.Complete(Guid.NewGuid());

        Assert.False(result);
    }
}
