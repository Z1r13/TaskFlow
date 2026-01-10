using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// GET /tasks
app.MapGet(
    "/tasks",
    async (AppDbContext db) =>
    {
        var tasks = await db.Tasks.ToListAsync();
        var responce = tasks.Select(x => new TaskResponse(
            x.Id,
            x.Title,
            x.Description,
            x.IsCompleted,
            x.CreatedAt
        ));

        return Results.Ok(responce);
    }
);

// POST /tasks
app.MapPost(
    "/tasks",
    async (CreateTaskRequest request, AppDbContext db) =>
    {
        var task = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        var responce = new TaskResponse(
            task.Id,
            task.Title,
            task.Description,
            task.IsCompleted,
            task.CreatedAt
        );
        return Results.Created($"/tasks/{task.Id}", responce);
    }
);

// POST /tasks/{id}
app.MapPost(
    "/tasks/{id:guid}",
    async (Guid id, UpdateTaskRequest request, AppDbContext db) =>
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null)
            return Results.NotFound();

        task.Title = request.Title;
        task.Description = request.Description;
        task.IsCompleted = request.IsCompleted;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }
);

// DELETE /tasks/{id}
app.MapDelete(
    "/tasks/{id:guid}",
    async (Guid id, AppDbContext db) =>
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null)
            return Results.NotFound();

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
);

app.Run();
