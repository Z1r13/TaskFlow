namespace TaskFlow.Api.Models;

public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Desription { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
