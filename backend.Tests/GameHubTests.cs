using System.Net.Http.Json;
using FourInRow.Server.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace FourInRow.Server.Tests;

public class GameHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GameHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task JoinGame_with_valid_token_claims_seat()
    {
        var created = await CreateGameAsync();
        await using var connection = await ConnectHubAsync();

        var result = await connection.InvokeAsync<JoinGameResult?>(
            "JoinGame",
            created.GameId,
            created.HostJoinToken);

        Assert.NotNull(result);
        Assert.Equal("Host", result.Seat);
        Assert.Equal(created.GuestJoinToken, result.PeerJoinToken);
    }

    [Fact]
    public async Task JoinGame_with_wrong_token_sends_error()
    {
        var created = await CreateGameAsync();
        await using var connection = await ConnectHubAsync();
        var error = WaitForError(connection);

        var result = await connection.InvokeAsync<JoinGameResult?>(
            "JoinGame",
            created.GameId,
            "not-a-real-token");

        Assert.Null(result);
        Assert.Equal("Invalid join token.", await error);
    }

    [Fact]
    public async Task JoinGame_rejects_second_client_for_same_seat()
    {
        var created = await CreateGameAsync();
        await using var first = await ConnectHubAsync();
        await using var second = await ConnectHubAsync();

        var firstResult = await first.InvokeAsync<JoinGameResult?>(
            "JoinGame",
            created.GameId,
            created.GuestJoinToken);
        Assert.NotNull(firstResult);

        var error = WaitForError(second);

        var secondResult = await second.InvokeAsync<JoinGameResult?>(
            "JoinGame",
            created.GameId,
            created.GuestJoinToken);

        Assert.Null(secondResult);
        Assert.Equal("That seat is already taken.", await error);
    }

    private static Task<string> WaitForError(HubConnection connection)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<string>("Error", message => tcs.TrySetResult(message));
        return tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private async Task<CreateGameResponse> CreateGameAsync()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/games",
            new CreateGameRequest("Alice", "Bob"));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateGameResponse>();
        Assert.NotNull(body);
        return body;
    }

    private async Task<HubConnection> ConnectHubAsync()
    {
        var server = _factory.Server;
        var connection = new HubConnectionBuilder()
            .WithUrl(
                "http://localhost/hubs/game",
                options => options.HttpMessageHandlerFactory = _ => server.CreateHandler())
            .Build();

        await connection.StartAsync();
        return connection;
    }
}
