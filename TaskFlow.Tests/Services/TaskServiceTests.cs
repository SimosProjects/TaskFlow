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
    /// Each test gets its own database instance to prevent state leaking between tests.
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

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

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

    // -------------------------------------------------------------------------
    // GetByIdAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that GetByIdAsync returns null when no task exists with the given ID.
    /// The controller relies on this null return to produce a 404 — the service must
    /// not throw or return a default object for unknown IDs.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_Returns_Null_For_Unknown_Id()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var result = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // GetAllAsync
    // -------------------------------------------------------------------------

    /// <summary>
    /// Ensures GetAllAsync returns all tasks when no filter is applied.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Returns_All_Created_Tasks()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        await service.CreateAsync(new CreateTaskRequest { Title = "T1", Description = null });
        await service.CreateAsync(new CreateTaskRequest { Title = "T2", Description = "D2" });

        var result = await service.GetAllAsync(new TaskQueryParameters
        {
            Page = 1,
            PageSize = 50,
            IsCompleted = null,
            Search = null
        });

        Assert.Equal(2, result.Items.Count);
    }

    /// <summary>
    /// Verifies that the IsCompleted filter correctly returns only matching tasks.
    /// This path is exercised separately from the no-filter path because the
    /// Where clause is conditionally applied — it must be explicitly tested.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_Filters_By_IsCompleted()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        var task1 = await service.CreateAsync(new CreateTaskRequest { Title = "Incomplete task" });
        var task2 = await service.CreateAsync(new CreateTaskRequest { Title = "Complete task" });

        await service.CompleteAsync(task2.Id);

        // Filter to completed only — should return exactly one result.
        var completedResult = await service.GetAllAsync(new TaskQueryParameters
        {
            Page = 1,
            PageSize = 50,
            IsCompleted = true
        });

        var completedItem = Assert.Single(completedResult.Items);
        Assert.Equal(task2.Id, completedItem.Id);
        Assert.True(completedItem.IsCompleted);

        // Filter to incomplete only — should return the other task.
        var incompleteResult = await service.GetAllAsync(new TaskQueryParameters
        {
            Page = 1,
            PageSize = 50,
            IsCompleted = false
        });

        var incompleteItem = Assert.Single(incompleteResult.Items);
        Assert.Equal(task1.Id, incompleteItem.Id);
        Assert.False(incompleteItem.IsCompleted);
    }

    /// <summary>
    /// Verifies NormalizePaging edge cases:
    ///   - page=0 should be treated as page 1 (guard against invalid input)
    ///   - pageSize=200 should be capped at 100 (guard against unbounded queries)
    ///
    /// These are important production guards — without them a caller could request
    /// page 0 (undefined offset behavior) or dump the entire table in one query.
    /// </summary>
    [Fact]
    public async Task GetAllAsync_NormalizePaging_Clamps_Invalid_Page_And_Oversized_PageSize()
    {
        var service = CreateService(out var db);
        await using var _ = db;

        // Seed 3 tasks so there is something to page over.
        await service.CreateAsync(new CreateTaskRequest { Title = "T1" });
        await service.CreateAsync(new CreateTaskRequest { Title = "T2" });
        await service.CreateAsync(new CreateTaskRequest { Title = "T3" });

        // page=0 should be normalized to page=1.
        var resultInvalidPage = await service.GetAllAsync(new TaskQueryParameters
        {
            Page = 0,
            PageSize = 20
        });

        Assert.Equal(1, resultInvalidPage.Page);
        Assert.Equal(3, resultInvalidPage.Items.Count);

        // pageSize=200 should be capped to 100.
        var resultOversizedPage = await service.GetAllAsync(new TaskQueryParameters
        {
            Page = 1,
            PageSize = 200
        });

        Assert.Equal(100, resultOversizedPage.PageSize);

        // All 3 tasks still returned since total < cap.
        Assert.Equal(3, resultOversizedPage.Items.Count);
    }

    // -------------------------------------------------------------------------
    // CompleteAsync
    // -------------------------------------------------------------------------

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