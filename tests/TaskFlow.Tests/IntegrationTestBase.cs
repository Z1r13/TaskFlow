namespace TaskFlow.Tests;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TaskFlow.Api.Contracts;
using TaskFlow.Api.Data;

public class IntegrationTestBase : IDisposable
{
    protected readonly HttpClient Client;
    protected readonly AppDbContext DbContext;
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTestBase()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase");
                });

                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.Database.EnsureCreated();
            });
        });

        Client = _factory.CreateClient();

        var scope = _factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected async Task<string> RegisterAndLoginUserAsync(
        string name = "Test User",
        string email = "test@example.com",
        string password = "password123"
    )
    {
        var registerRequest = new
        {
            name = name,
            email = email,
            password = password,
        };

        var registerResponse = await Client.PostAsJsonAsync("/auth/register", registerRequest);

        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new Exception(
                $"Registration failed: {registerResponse.StatusCode}, {errorContent}"
            );
        }

        var loginRequest = new { email = email, password = password };

        var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);

        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new Exception($"Login failed: {loginResponse.StatusCode}, {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        if (string.IsNullOrEmpty(loginResult?.Token))
        {
            throw new Exception("Token is null or empty in login response");
        }

        return loginResult.Token;
    }

    protected void SetAuthorizationHeader(string token)
    {
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
        Client.Dispose();
        _factory.Dispose();
    }
}
