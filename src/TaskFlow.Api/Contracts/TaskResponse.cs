using TaskFlow.Api.Models;

namespace TaskFlow.Api.Contracts;

public record TaskResponse(Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTime CreatedAt
    )
{
    public static TaskResponse From(TaskItem task) =>
        new(task.Id, task.Title, task.Desription, task.IsCompleted, task.CreatedAt);
}


