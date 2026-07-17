using FourInRow.Server.Models;
using FourInRow.Server.Services;

namespace FourInRow.Server.Tests;

public class GameEngineTests
{
    private readonly GameEngine _engine = new();

    [Fact]
    public void TryDropDisc_rejects_when_game_not_in_progress()
    {
        var room = NewRoom(GameStatus.WaitingForPlayers);

        var (ok, error) = _engine.TryDropDisc(room, PlayerSlot.Host, 0);

        Assert.False(ok);
        Assert.Equal("Game is not in progress.", error);
        Assert.Equal(0, room.Board[0, 0]);
    }

    [Fact]
    public void TryDropDisc_rejects_when_not_players_turn()
    {
        var room = NewRoom();

        var (ok, error) = _engine.TryDropDisc(room, PlayerSlot.Guest, 0);

        Assert.False(ok);
        Assert.Equal("Not your turn.", error);
        Assert.Equal(PlayerSlot.Host, room.CurrentTurn);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void TryDropDisc_rejects_column_out_of_range(int column)
    {
        var room = NewRoom();

        var (ok, error) = _engine.TryDropDisc(room, PlayerSlot.Host, column);

        Assert.False(ok);
        Assert.Equal("Column out of range.", error);
    }

    [Fact]
    public void TryDropDisc_rejects_full_column()
    {
        var room = NewRoom();
        for (var row = 0; row < GameRoom.Rows; row++)
        {
            room.Board[row, 3] = row % 2 == 0 ? (int)PlayerSlot.Host : (int)PlayerSlot.Guest;
        }

        var (ok, error) = _engine.TryDropDisc(room, PlayerSlot.Host, 3);

        Assert.False(ok);
        Assert.Equal("Column is full.", error);
    }

    [Fact]
    public void TryDropDisc_places_disc_and_switches_turn()
    {
        var room = NewRoom();

        var (ok, error) = _engine.TryDropDisc(room, PlayerSlot.Host, 2);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal((int)PlayerSlot.Host, room.Board[0, 2]);
        Assert.Equal(PlayerSlot.Guest, room.CurrentTurn);
        Assert.Equal(GameStatus.InProgress, room.Status);
    }

    [Fact]
    public void TryDropDisc_stacks_discs_upward_in_column()
    {
        var room = NewRoom();

        _engine.TryDropDisc(room, PlayerSlot.Host, 1);
        room.CurrentTurn = PlayerSlot.Guest;
        _engine.TryDropDisc(room, PlayerSlot.Guest, 1);

        Assert.Equal((int)PlayerSlot.Host, room.Board[0, 1]);
        Assert.Equal((int)PlayerSlot.Guest, room.Board[1, 1]);
    }

    [Fact]
    public void TryDropDisc_detects_horizontal_win()
    {
        var room = NewRoom();
        room.Board[0, 0] = (int)PlayerSlot.Host;
        room.Board[0, 1] = (int)PlayerSlot.Host;
        room.Board[0, 2] = (int)PlayerSlot.Host;

        var (ok, error) = _engine.TryDropDisc(room, PlayerSlot.Host, 3);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(GameStatus.HostWon, room.Status);
        Assert.Equal(PlayerSlot.Host, room.Winner);
    }

    [Fact]
    public void TryDropDisc_detects_vertical_win()
    {
        var room = NewRoom();
        room.Board[0, 4] = (int)PlayerSlot.Guest;
        room.Board[1, 4] = (int)PlayerSlot.Guest;
        room.Board[2, 4] = (int)PlayerSlot.Guest;
        room.CurrentTurn = PlayerSlot.Guest;

        var (ok, _) = _engine.TryDropDisc(room, PlayerSlot.Guest, 4);

        Assert.True(ok);
        Assert.Equal(GameStatus.GuestWon, room.Status);
        Assert.Equal(PlayerSlot.Guest, room.Winner);
    }

    [Fact]
    public void TryDropDisc_detects_diagonal_up_right_win()
    {
        var room = NewRoom();
        // Diagonal: (0,0) (1,1) (2,2) — winning drop at (3,3)
        room.Board[0, 0] = (int)PlayerSlot.Host;
        room.Board[0, 1] = (int)PlayerSlot.Guest;
        room.Board[1, 1] = (int)PlayerSlot.Host;
        room.Board[0, 2] = (int)PlayerSlot.Guest;
        room.Board[1, 2] = (int)PlayerSlot.Guest;
        room.Board[2, 2] = (int)PlayerSlot.Host;
        room.Board[0, 3] = (int)PlayerSlot.Guest;
        room.Board[1, 3] = (int)PlayerSlot.Guest;
        room.Board[2, 3] = (int)PlayerSlot.Guest;

        var (ok, _) = _engine.TryDropDisc(room, PlayerSlot.Host, 3);

        Assert.True(ok);
        Assert.Equal(GameStatus.HostWon, room.Status);
        Assert.Equal((int)PlayerSlot.Host, room.Board[3, 3]);
    }

    [Fact]
    public void TryDropDisc_detects_diagonal_up_left_win()
    {
        var room = NewRoom();
        // Diagonal: (0,3) (1,2) (2,1) — winning drop at (3,0)
        room.Board[0, 3] = (int)PlayerSlot.Host;
        room.Board[0, 2] = (int)PlayerSlot.Guest;
        room.Board[1, 2] = (int)PlayerSlot.Host;
        room.Board[0, 1] = (int)PlayerSlot.Guest;
        room.Board[1, 1] = (int)PlayerSlot.Guest;
        room.Board[2, 1] = (int)PlayerSlot.Host;
        room.Board[0, 0] = (int)PlayerSlot.Guest;
        room.Board[1, 0] = (int)PlayerSlot.Guest;
        room.Board[2, 0] = (int)PlayerSlot.Guest;

        var (ok, _) = _engine.TryDropDisc(room, PlayerSlot.Host, 0);

        Assert.True(ok);
        Assert.Equal(GameStatus.HostWon, room.Status);
        Assert.Equal((int)PlayerSlot.Host, room.Board[3, 0]);
    }

    [Fact]
    public void TryDropDisc_detects_draw_when_board_fills_without_winner()
    {
        var room = NewRoom();
        FillBoardWithoutFourInARow(room.Board);
        // Leave top of column 6 empty for the final host drop
        room.Board[GameRoom.Rows - 1, 6] = 0;

        var (ok, _) = _engine.TryDropDisc(room, PlayerSlot.Host, 6);

        Assert.True(ok);
        Assert.Equal(GameStatus.Draw, room.Status);
        Assert.Equal(PlayerSlot.None, room.Winner);
    }

    private static GameRoom NewRoom(GameStatus status = GameStatus.InProgress) =>
        new()
        {
            Id = "test-game",
            HostName = "Alice",
            GuestName = "Bob",
            HostJoinToken = "host-token",
            GuestJoinToken = "guest-token",
            Status = status,
            CurrentTurn = PlayerSlot.Host,
        };

    /// <summary>
    /// Alternating 3+3 column pattern that avoids four-in-a-row on a full board.
    /// Columns cycle Host/Guest/Host/Guest… by blocks so no line of 4 forms.
    /// </summary>
    private static void FillBoardWithoutFourInARow(int[,] board)
    {
        // Pattern per column (bottom→top), chosen so no horizontal/vertical/diagonal four exists.
        int[][] columns =
        [
            [1, 1, 1, 2, 2, 2],
            [2, 2, 2, 1, 1, 1],
            [1, 1, 1, 2, 2, 2],
            [2, 2, 2, 1, 1, 1],
            [1, 1, 1, 2, 2, 2],
            [2, 2, 2, 1, 1, 1],
            [1, 1, 1, 2, 2, 2],
        ];

        for (var col = 0; col < GameRoom.Columns; col++)
        {
            for (var row = 0; row < GameRoom.Rows; row++)
            {
                board[row, col] = columns[col][row];
            }
        }
    }
}
