using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.DTOs;

public class CreateTaskRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}
