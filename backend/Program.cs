using FourInRow.Server.Hubs;
using FourInRow.Server.Models;
using FourInRow.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameStore>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:4173",
                "http://127.0.0.1:4173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/games", (CreateGameRequest request, GameStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.HostName) || string.IsNullOrWhiteSpace(request.GuestName))
    {
        return Results.BadRequest(new { error = "HostName and GuestName are required." });
    }

    if (string.Equals(request.HostName.Trim(), request.GuestName.Trim(), StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Host and guest names must be different." });
    }

    var room = store.Create(request.HostName, request.GuestName);
    var response = new CreateGameResponse(room.Id, room.HostName, room.GuestName);
    return Results.Created($"/api/games/{room.Id}", response);
});

app.MapGet("/api/games/{gameId}", (string gameId, GameStore store) =>
{
    var room = store.Get(gameId);
    return room is null ? Results.NotFound() : Results.Ok(room.ToDto());
});

app.MapHub<GameHub>("/hubs/game");

app.Run();

public partial class Program;
