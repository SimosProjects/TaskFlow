using TaskFlow.Api.Domain;
using Xunit;

namespace TaskFlow.Tests.Domain;

/// <summary>
/// Unit tests for the TaskItem domain model.
/// These tests validate domain invariants and behavior.
/// </summary>
public class TaskItemTests
{
    [Fact]
    public void Constructor_Throws_When_Title_Is_Whitespace()
    {
        // Arrange
        var invalidTitle = "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TaskItem(invalidTitle));
    }

    [Fact]
    public void MarkComplete_Is_Idempotent()
    {
        var task = new TaskItem("Test task");

        task.MarkComplete();
        task.MarkComplete();

        Assert.True(task.IsCompleted);
    }
}
