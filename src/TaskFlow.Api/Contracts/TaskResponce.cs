using TaskFlow.Api.Models;

namespace TaskFlow.Api.Contracts;

public record TaskResponce(Guid Id,
        string Title,
        string? Description,
        bool IsCompleted,
        DateTime CreatedAt
    )
{
    public static TaskResponce From(TaskItem task) =>
        new(task.Id, task.Title, task.Desription, task.IsCompleted, task.CreatedAt);
}


