using FourInRow.Server.Models;
using FourInRow.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace FourInRow.Server.Hubs;

public sealed class GameHub(GameStore store, GameEngine engine) : Hub
{
    public async Task JoinGame(string gameId, string playerName)
    {
        var room = store.Get(gameId);
        if (room is null)
        {
            await Clients.Caller.SendAsync("Error", "Game not found.");
            return;
        }

        var name = playerName.Trim();
        var slot = ResolveSlot(room, name);
        if (slot == PlayerSlot.None)
        {
            await Clients.Caller.SendAsync(
                "Error",
                $"Name must be '{room.HostName}' or '{room.GuestName}'.");
            return;
        }

        var existingConnection = slot == PlayerSlot.Host ? room.HostConnectionId : room.GuestConnectionId;
        if (!string.IsNullOrEmpty(existingConnection) && existingConnection != Context.ConnectionId)
        {
            await Clients.Caller.SendAsync("Error", "That seat is already taken.");
            return;
        }

        if (slot == PlayerSlot.Host)
        {
            room.HostConnectionId = Context.ConnectionId;
        }
        else
        {
            room.GuestConnectionId = Context.ConnectionId;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id);

        if (room.BothPlayersConnected && room.Status == GameStatus.WaitingForPlayers)
        {
            room.Status = GameStatus.InProgress;
        }

        await Clients.Group(room.Id).SendAsync("GameUpdated", room.ToDto());
    }

    public async Task DropDisc(string gameId, int column)
    {
        var room = store.Get(gameId);
        if (room is null)
        {
            await Clients.Caller.SendAsync("Error", "Game not found.");
            return;
        }

        var slot = SlotForConnection(room, Context.ConnectionId);
        if (slot == PlayerSlot.None)
        {
            await Clients.Caller.SendAsync("Error", "You are not in this game.");
            return;
        }

        var (ok, error) = engine.TryDropDisc(room, slot, column);
        if (!ok)
        {
            await Clients.Caller.SendAsync("Error", error);
            return;
        }

        await Clients.Group(room.Id).SendAsync("GameUpdated", room.ToDto());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var room in store.FindByConnection(Context.ConnectionId))
        {
            if (room.HostConnectionId == Context.ConnectionId)
            {
                room.HostConnectionId = null;
            }

            if (room.GuestConnectionId == Context.ConnectionId)
            {
                room.GuestConnectionId = null;
            }

            if (room.Status == GameStatus.InProgress)
            {
                room.Status = GameStatus.WaitingForPlayers;
            }

            await Clients.Group(room.Id).SendAsync("GameUpdated", room.ToDto());
        }

        await base.OnDisconnectedAsync(exception);
    }

    private static PlayerSlot ResolveSlot(GameRoom room, string name)
    {
        if (NamesMatch(name, room.HostName))
        {
            return PlayerSlot.Host;
        }

        if (NamesMatch(name, room.GuestName))
        {
            return PlayerSlot.Guest;
        }

        return PlayerSlot.None;
    }

    private static PlayerSlot SlotForConnection(GameRoom room, string connectionId)
    {
        if (room.HostConnectionId == connectionId)
        {
            return PlayerSlot.Host;
        }

        if (room.GuestConnectionId == connectionId)
        {
            return PlayerSlot.Guest;
        }

        return PlayerSlot.None;
    }

    private static bool NamesMatch(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
