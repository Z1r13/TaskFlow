using Microsoft.AspNetCore.Http.HttpResults;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var tasks = new List<TaskItem>();

// GET /tasks
app.MapGet("/tasks", () =>
{
    return Results.Ok(tasks.Select(x => new TaskResponse(
        x.Id, x.Title, x.Desсription, x.IsCompleted, x.CreatedAt)));
});

// POST /tasks
app.MapPost("/tasks", (CreateTaskRequest request) =>
{
    var task = new TaskItem
    {
        Id = Guid.NewGuid(),
        Title = request.Title,
        Desсription = request.Description,
        IsCompleted = false,
        CreatedAt = DateTime.Now
    };

    tasks.Add(task);

    var responce = TaskResponse.From(task);
    return Results.Created($"/tasks/{task.Id}", responce);
});

// POST /tasks/{id}
app.MapPost("/tasks/{id:guid}", (Guid id, UpdateTaskRequest request) =>
{
    var task = tasks.FirstOrDefault(x => x.Id == id);
    if (task == null)
        return Results.NotFound();

    task.Title = request.Title;
    task.Desсription = request.Description;
    task.IsCompleted = request.IsCompleted;

    return Results.NoContent();
});

// DELETE /tasks/{id}
app.MapDelete("/tasks/{id:guid}", (Guid id) =>
{
    var task = tasks.FirstOrDefault(x => x.Id == id);
    if (task == null)
        return Results.NotFound();

    tasks.Remove(task);

    return Results.NoContent();
});

app.Run();
