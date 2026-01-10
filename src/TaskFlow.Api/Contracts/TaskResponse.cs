namespace TaskFlow.Api.Contracts;

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTime CreatedAt
);
