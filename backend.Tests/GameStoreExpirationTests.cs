using FourInRow.Server.Models;
using FourInRow.Server.Services;

namespace FourInRow.Server.Tests;

public class GameStoreExpirationTests
{
    [Fact]
    public void Get_returns_null_and_removes_expired_active_room()
    {
        var store = new GameStore();
        var room = store.Create("Alice", "Bob");
        room.LastActivityAt = DateTimeOffset.UtcNow - GameRoom.ActiveTtl;

        Assert.Null(store.Get(room.Id));
        Assert.Empty(store.All());
    }

    [Fact]
    public void Ended_room_expires_sooner_than_active_room()
    {
        var store = new GameStore();
        var active = store.Create("Alice", "Bob");
        var ended = store.Create("Carol", "Dave");
        ended.Status = GameStatus.HostWon;
        ended.Winner = PlayerSlot.Host;

        var pastEndedTtl = DateTimeOffset.UtcNow - GameRoom.EndedTtl - TimeSpan.FromSeconds(1);
        active.LastActivityAt = pastEndedTtl;
        ended.LastActivityAt = pastEndedTtl;

        Assert.NotNull(store.Get(active.Id));
        Assert.Null(store.Get(ended.Id));
    }

    [Fact]
    public void RemoveExpired_clears_only_stale_rooms()
    {
        var store = new GameStore();
        var fresh = store.Create("Alice", "Bob");
        var stale = store.Create("Carol", "Dave");
        stale.Status = GameStatus.Draw;
        stale.LastActivityAt = DateTimeOffset.UtcNow - GameRoom.EndedTtl - TimeSpan.FromMinutes(1);

        var removed = store.RemoveExpired();

        Assert.Equal(1, removed);
        Assert.NotNull(store.Get(fresh.Id));
        Assert.Null(store.Get(stale.Id));
    }

    [Fact]
    public void Touch_extends_active_lifetime()
    {
        var store = new GameStore();
        var room = store.Create("Alice", "Bob");
        room.LastActivityAt = DateTimeOffset.UtcNow - GameRoom.ActiveTtl + TimeSpan.FromSeconds(5);

        room.Touch();

        Assert.NotNull(store.Get(room.Id));
    }
}
