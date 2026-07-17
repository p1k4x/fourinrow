using System.Collections.Concurrent;
using FourInRow.Server.Models;

namespace FourInRow.Server.Services;

public sealed class GameStore
{
    private readonly ConcurrentDictionary<string, GameRoom> _games = new();

    public GameRoom Create(string hostName, string guestName)
    {
        var id = GenerateId();
        var now = DateTimeOffset.UtcNow;
        var room = new GameRoom
        {
            Id = id,
            HostName = hostName.Trim(),
            GuestName = guestName.Trim(),
            HostJoinToken = GenerateToken(),
            GuestJoinToken = GenerateToken(),
            CreatedAt = now,
            LastActivityAt = now
        };

        if (!_games.TryAdd(id, room))
        {
            throw new InvalidOperationException("Failed to create game room.");
        }

        return room;
    }

    public GameRoom? Get(string gameId)
    {
        if (!_games.TryGetValue(gameId, out var room))
        {
            return null;
        }

        if (room.IsExpired(DateTimeOffset.UtcNow))
        {
            _games.TryRemove(gameId, out _);
            return null;
        }

        return room;
    }

    public int RemoveExpired(DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;
        // Snapshot before remove — Values is not a moment-in-time view under concurrent writes.
        var expiredIds = _games
            .Where(pair => pair.Value.IsExpired(now))
            .Select(pair => pair.Key)
            .ToArray();

        var removed = 0;
        foreach (var id in expiredIds)
        {
            if (_games.TryRemove(id, out _))
            {
                removed++;
            }
        }

        return removed;
    }

    public IEnumerable<GameRoom> All() => _games.Values;

    public IEnumerable<GameRoom> FindByConnection(string connectionId) =>
        _games.Values.Where(r =>
            r.HostConnectionId == connectionId || r.GuestConnectionId == connectionId);

    private static string GenerateId() =>
        Convert.ToHexString(Guid.NewGuid().ToByteArray())[..10].ToLowerInvariant();

    private static string GenerateToken() =>
        Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
}
