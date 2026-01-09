using TaskFlow.Api.Data;

namespace TaskFlow.Api.Contracts;

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt
)
{
    public static TaskResponse From(TaskEntity task) =>
        new(task.Id, task.Title, task.Description, task.IsCompleted, task.CreatedAt);
}
