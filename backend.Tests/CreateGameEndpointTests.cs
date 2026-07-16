using System.Net;
using System.Net.Http.Json;
using FourInRow.Server.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FourInRow.Server.Tests;

public class CreateGameEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateGameEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_api_games_creates_room()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/games",
            new CreateGameRequest("Alice", "Bob"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.GameId));
        Assert.Equal("Alice", body.HostName);
        Assert.Equal("Bob", body.GuestName);
        Assert.Contains($"/api/games/{body.GameId}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_api_games_rejects_blank_names()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/games",
            new CreateGameRequest(" ", "Bob"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_api_games_rejects_same_names()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/games",
            new CreateGameRequest("Alice", "alice"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
