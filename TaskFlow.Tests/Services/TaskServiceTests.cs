using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskFlow.Api.DTOs;
using TaskFlow.Api.Infrastructure;
using TaskFlow.Api.Services;
using Xunit;

namespace TaskFlow.Tests.Services;

/// <summary>
/// Unit-style tests for TaskService using EF Core InMemory provider.
/// These validate application-layer behavior without touching real Postgres.
/// </summary>
public class TaskServiceTests
{
    /// <summary>
    /// Creates a TaskService instance backed by an isolated in-memory EF Core database.
    /// </summary>
    private static TaskService CreateService(out TaskFlowDbContext db)
    {
        var options = new DbContextOptionsBuilder<TaskFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"taskflow-tests-{Guid.NewGuid():N}")
            .Options;

        db = new TaskFlowDbContext(options);

        // Ensures the model is initialized and database created for this test instance.
        db.Database.EnsureCreated();

        return new TaskService(db, NullLogger<TaskService>.Instance);
    }

    /// <summary>
    /// Verifies that creating a task persists it and allows retrieval by ID.
    /// </summary>
    [Fact]
    public async Task CreateAsync_Adds_Task_And_Can_Be_Retrieved_By_Id()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateAsync(new CreateTaskRequest
        {
            Title = "Title",
            Description = "Desc"
        });

        var fetched = await service.GetByIdAsync(created.Id);

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Title", fetched.Title);
        Assert.Equal("Desc", fetched.Description);
        Assert.False(fetched.IsCompleted);
    }

    /// <summary>
    /// Ensures GetAllAsync returns tasks created during the service lifetime.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns_All_Created_Tasks()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        await service.CreateAsync(new CreateTaskRequest { Title = "T1", Description = null });
        await service.CreateAsync(new CreateTaskRequest { Title = "T2", Description = "D2" });

        // Avoid Search because ILIKE translation may vary by provider.
        var result = await service.GetAllAsync(new TaskQueryParameters
        {
            Page = 1,
            PageSize = 50,
            IsCompleted = null,
            Search = null
        });

        // Assumes PagedResult<T> exposes Items (common pattern).
        Assert.Equal(2, result.Items.Count);
    }

    /// <summary>
    /// Verifies that CompleteAsync returns false when the task does not exist.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_Returns_False_When_Task_Does_Not_Exist()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var result = await service.CompleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    /// <summary>
    /// Ensures CompleteAsync transitions an existing task to completed.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_Marks_Task_As_Completed_When_It_Exists()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var created = await service.CreateAsync(new CreateTaskRequest
        {
            Title = "Title",
            Description = null
        });

        var result = await service.CompleteAsync(created.Id);

        Assert.True(result);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.True(fetched!.IsCompleted);
    }
}