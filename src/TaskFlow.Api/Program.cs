using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseStatusCodePages();

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
    async (CreateTaskRequest request, AppDbContext db, IValidator<CreateTaskRequest> validator) =>
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

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
    async (
        Guid id,
        UpdateTaskRequest request,
        AppDbContext db,
        IValidator<UpdateTaskRequest> validator
    ) =>
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var task = await db.Tasks.FindAsync(id);
        if (task is null)
            return Results.Problem(
                title: "Task not found",
                detail: $"Task {id} doesn't exist",
                statusCode: StatusCodes.Status404NotFound
            );

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
            return Results.Problem(
                title: "Task not found",
                detail: $"Task {id} doesn't exist",
                statusCode: StatusCodes.Status404NotFound
            );

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
);

app.Run();
