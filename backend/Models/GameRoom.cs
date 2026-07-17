namespace FourInRow.Server.Models;

public sealed class GameRoom
{
    public const int Rows = 6;
    public const int Columns = 7;

    public required string Id { get; init; }
    public required string HostName { get; init; }
    public required string GuestName { get; init; }
    public required string HostJoinToken { get; init; }
    public required string GuestJoinToken { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public string? HostConnectionId { get; set; }
    public string? GuestConnectionId { get; set; }

    /// <summary>0 = empty, 1 = host, 2 = guest. Index is [row, column], row 0 is the bottom.</summary>
    public int[,] Board { get; } = new int[Rows, Columns];

    public PlayerSlot CurrentTurn { get; set; } = PlayerSlot.Host;
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    public PlayerSlot Winner { get; set; } = PlayerSlot.None;

    public bool BothPlayersConnected =>
        !string.IsNullOrEmpty(HostConnectionId) && !string.IsNullOrEmpty(GuestConnectionId);

    public GameStateDto ToDto() => new(
        Id,
        HostName,
        GuestName,
        HostConnected: !string.IsNullOrEmpty(HostConnectionId),
        GuestConnected: !string.IsNullOrEmpty(GuestConnectionId),
        FlattenBoard(Board),
        CurrentTurn.ToString(),
        Status.ToString(),
        Winner == PlayerSlot.None ? null : Winner.ToString());

    private static int[] FlattenBoard(int[,] board)
    {
        var flat = new int[Rows * Columns];
        for (var row = 0; row < Rows; row++)
        {
            for (var col = 0; col < Columns; col++)
            {
                flat[row * Columns + col] = board[row, col];
            }
        }

        return flat;
    }
}

public sealed record GameStateDto(
    string GameId,
    string HostName,
    string GuestName,
    bool HostConnected,
    bool GuestConnected,
    int[] Board,
    string CurrentTurn,
    string Status,
    string? Winner);

public sealed record CreateGameRequest(string HostName, string GuestName);

public sealed record CreateGameResponse(
    string GameId,
    string HostName,
    string GuestName,
    string HostJoinToken,
    string GuestJoinToken);

public sealed record JoinGameResult(string Seat, string PeerJoinToken);
