using FourInRow.Server.Models;
using FourInRow.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace FourInRow.Server.Hubs;

public sealed class GameHub(GameStore store, GameEngine engine) : Hub
{
    public async Task<JoinGameResult?> JoinGame(string gameId, string joinToken)
    {
        var room = store.Get(gameId);
        if (room is null)
        {
            await Clients.Caller.SendAsync("Error", "Game not found.");
            return null;
        }

        var token = joinToken.Trim();
        var slot = ResolveSlot(room, token);
        if (slot == PlayerSlot.None)
        {
            await Clients.Caller.SendAsync("Error", "Invalid join token.");
            return null;
        }

        var existingConnection = slot == PlayerSlot.Host ? room.HostConnectionId : room.GuestConnectionId;
        if (!string.IsNullOrEmpty(existingConnection) && existingConnection != Context.ConnectionId)
        {
            await Clients.Caller.SendAsync("Error", "That seat is already taken.");
            return null;
        }

        if (slot == PlayerSlot.Host)
        {
            room.HostConnectionId = Context.ConnectionId;
        }
        else
        {
            room.GuestConnectionId = Context.ConnectionId;
        }

        room.Touch();

        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id);

        if (room.BothPlayersConnected && room.Status == GameStatus.WaitingForPlayers)
        {
            room.Status = GameStatus.InProgress;
        }

        await Clients.Group(room.Id).SendAsync("GameUpdated", room.ToDto());

        var peerToken = slot == PlayerSlot.Host ? room.GuestJoinToken : room.HostJoinToken;
        return new JoinGameResult(slot.ToString(), peerToken);
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

        room.Touch();
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

    private static PlayerSlot ResolveSlot(GameRoom room, string token)
    {
        if (TokensMatch(token, room.HostJoinToken))
        {
            return PlayerSlot.Host;
        }

        if (TokensMatch(token, room.GuestJoinToken))
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

    private static bool TokensMatch(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.Ordinal);
}
