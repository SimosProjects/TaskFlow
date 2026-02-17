using TaskFlow.Api.Domain;

namespace TaskFlow.Api.DTOs;

/// <summary>
/// Central mapping helpers between internal domain models and public API DTOs.
/// Keeps controllers thin and prevents leaking domain objects into API contracts.
/// </summary>
public static class TaskMapping
{
    /// <summary>
    /// Maps a domain TaskItem into an API TaskResponse DTO.
    /// </summary>
    public static TaskResponse ToResponse(this TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        IsCompleted = task.IsCompleted,
        CreatedAtUtc = task.CreatedAtUtc
    };
}
