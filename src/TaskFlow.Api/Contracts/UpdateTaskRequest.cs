namespace TaskFlow.Api.Contracts;

public record UpdateTaskRequest(string Title, string? Description, bool IsCompleted);
