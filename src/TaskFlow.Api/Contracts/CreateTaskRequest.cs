namespace TaskFlow.Api.Contracts;

public record CreateTaskRequest(string Title, string? Description);

// public class CreateTaskRequest
// {
//     public string Title { get; set; } = string.Empty;
//     public string? Desription { get; set; }
// }
