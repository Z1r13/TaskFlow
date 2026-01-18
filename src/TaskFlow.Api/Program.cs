using System.Security.Claims;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskFlow.Api.Auth;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Определяем схему безопасности (как будем аутентифицироваться)
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Введите JWT токен в формате: Bearer {ваш токен}",
        }
    );

    // Требуем токен для всех эндпоинтов
    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});

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

builder.Services.AddSingleton<JwtTokenGenerator>();
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)
            ),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();
app.UseStatusCodePages();

// POST /auth/register
app.MapPost(
    "/auth/register",
    async (RegisterRequest request, AppDbContext db) =>
    {
        var userExist = await db.Users.AnyAsync(x => x.Email == request.Email);
        if (userExist)
            return Results.Conflict(new { message = "User with this email already exist" });

        var newUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        return Results.Created(
            $"/users/{newUser.Id}",
            new { userId = newUser.Id, email = newUser.Email }
        );
    }
);

// POST /auth/login
app.MapPost(
    "/auth/login",
    async (LoginRequest request, AppDbContext db, JwtTokenGenerator tokenGenerator) =>
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == request.Email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Results.Unauthorized();

        var token = tokenGenerator.Generate(user.Id, user.Email);

        return Results.Ok(new AuthResponse(token));
    }
);

// GET /tasks
app.MapGet(
        "/tasks",
        async (AppDbContext db, HttpContext httpCtx) =>
        {
            var userIdClaim = httpCtx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Results.Unauthorized();

            var tasks = await db.Tasks.Where(x => x.UserId == currentUserId).ToListAsync();

            var responce = tasks.Select(x => new TaskResponse(
                x.Id,
                x.Title,
                x.Description,
                x.IsCompleted,
                x.CreatedAt
            ));

            return Results.Ok(responce);
        }
    )
    .RequireAuthorization();

// POST /tasks
app.MapPost(
        "/tasks",
        async (
            CreateTaskRequest request,
            AppDbContext db,
            IValidator<CreateTaskRequest> validator,
            HttpContext httpCtx
        ) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
                return Results.ValidationProblem(validationResult.ToDictionary());

            var userIdClaim = httpCtx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Results.Unauthorized();

            var task = new TaskEntity
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
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
    )
    .RequireAuthorization();

// POST /tasks/{id}
app.MapPost(
        "/tasks/{id:guid}",
        async (
            Guid id,
            UpdateTaskRequest request,
            AppDbContext db,
            IValidator<UpdateTaskRequest> validator,
            HttpContext httpCtx
        ) =>
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var userIdClaim = httpCtx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Results.Unauthorized();

            var task = await db
                .Tasks.Where(x => x.Id == id && x.UserId == currentUserId)
                .FirstOrDefaultAsync();

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
    )
    .RequireAuthorization();

// DELETE /tasks/{id}
app.MapDelete(
        "/tasks/{id:guid}",
        async (Guid id, AppDbContext db, HttpContext httpCtx) =>
        {
            var userIdClaim = httpCtx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var currentUserId))
                return Results.Unauthorized();

            var task = await db
                .Tasks.Where(x => x.Id == id && x.UserId == currentUserId)
                .FirstOrDefaultAsync();

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
    )
    .RequireAuthorization();

app.Run();
