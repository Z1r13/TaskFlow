using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaskFlow.Api.Contracts;

namespace TaskFlow.Tests;

public class TaskEndpointsTest : IntegrationTestBase
{
    [Fact]
    public async Task CreateTask_ReturnsCreated_WhenValidRequest()
    {
        // Arrange
        var token = await RegisterAndLoginUserAsync();
        SetAuthorizationHeader(token);

        var request = new { title = "Test task", description = "Description" };

        // Act
        var response = await Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateTask_ReturnsBadRequest_WhenTitleEmpty()
    {
        // Arrange
        var token = await RegisterAndLoginUserAsync();
        SetAuthorizationHeader(token);

        var request = new { title = "", description = "Description" };

        // Act
        var response = await Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTask_ReturnsUnauthorized_WhenNoToken()
    {
        // Arrange
        var request = new { title = "Test", description = "Description" };

        // Act
        var response = await Client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
