using TaskFlow.Api.Domain;
using Xunit;

namespace TaskFlow.Tests.Domain;

/// <summary>
/// Unit tests for the TaskItem domain model.
/// 
/// These tests validate domain invariants and behavior independently
/// of HTTP, persistence, or infrastructure concerns.
/// </summary>
public class TaskItemTests
{
    /// <summary>
    /// Verifies that the constructor enforces the required Title invariant.
    /// The domain model must never allow an invalid task to exist.
    /// </summary>
    [Fact]
    public void Constructor_Throws_When_Title_Is_Whitespace()
    {
        Assert.Throws<ArgumentException>(() => new TaskItem("   "));
    }

    /// <summary>
    /// Ensures that Title is trimmed during construction.
    /// This keeps the domain model normalized and consistent.
    /// </summary>
    [Fact]
    public void Constructor_Trims_Title()
    {
        var task = new TaskItem("  Hello  ");

        Assert.Equal("Hello", task.Title);
    }

    /// <summary>
    /// Verifies that calling MarkComplete multiple times does not
    /// cause invalid state transitions (idempotent behavior).
    /// </summary>
    [Fact]
    public void MarkComplete_Is_Idempotent()
    {
        var task = new TaskItem("Test task");

        task.MarkComplete();
        task.MarkComplete();

        Assert.True(task.IsCompleted);
    }
}