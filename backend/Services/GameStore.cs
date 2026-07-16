using System.Collections.Concurrent;
using FourInRow.Server.Models;

namespace FourInRow.Server.Services;

public sealed class GameStore
{
    private readonly ConcurrentDictionary<string, GameRoom> _games = new();

    public GameRoom Create(string hostName, string guestName)
    {
        var id = GenerateId();
        var room = new GameRoom
        {
            Id = id,
            HostName = hostName.Trim(),
            GuestName = guestName.Trim()
        };

        if (!_games.TryAdd(id, room))
        {
            throw new InvalidOperationException("Failed to create game room.");
        }

        return room;
    }

    public GameRoom? Get(string gameId) =>
        _games.TryGetValue(gameId, out var room) ? room : null;

    public IEnumerable<GameRoom> All() => _games.Values;

    public IEnumerable<GameRoom> FindByConnection(string connectionId) =>
        _games.Values.Where(r =>
            r.HostConnectionId == connectionId || r.GuestConnectionId == connectionId);

    private static string GenerateId() =>
        Convert.ToHexString(Guid.NewGuid().ToByteArray())[..10].ToLowerInvariant();
}
