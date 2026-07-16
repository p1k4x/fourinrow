using FourInRow.Server.Models;

namespace FourInRow.Server.Services;

public sealed class GameEngine
{
    public (bool Ok, string? Error) TryDropDisc(GameRoom room, PlayerSlot player, int column)
    {
        if (room.Status != GameStatus.InProgress)
        {
            return (false, "Game is not in progress.");
        }

        if (player != room.CurrentTurn)
        {
            return (false, "Not your turn.");
        }

        if (column < 0 || column >= GameRoom.Columns)
        {
            return (false, "Column out of range.");
        }

        var row = FindDropRow(room.Board, column);
        if (row < 0)
        {
            return (false, "Column is full.");
        }

        room.Board[row, column] = (int)player;

        if (HasFourInARow(room.Board, row, column, (int)player))
        {
            room.Winner = player;
            room.Status = player == PlayerSlot.Host ? GameStatus.HostWon : GameStatus.GuestWon;
            return (true, null);
        }

        if (IsBoardFull(room.Board))
        {
            room.Status = GameStatus.Draw;
            return (true, null);
        }

        room.CurrentTurn = player == PlayerSlot.Host ? PlayerSlot.Guest : PlayerSlot.Host;
        return (true, null);
    }

    private static int FindDropRow(int[,] board, int column)
    {
        for (var row = 0; row < GameRoom.Rows; row++)
        {
            if (board[row, column] == 0)
            {
                return row;
            }
        }

        return -1;
    }

    private static bool IsBoardFull(int[,] board)
    {
        for (var col = 0; col < GameRoom.Columns; col++)
        {
            if (board[GameRoom.Rows - 1, col] == 0)
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasFourInARow(int[,] board, int row, int col, int player)
    {
        return Count(board, row, col, player, 0, 1) + Count(board, row, col, player, 0, -1) - 1 >= 4
            || Count(board, row, col, player, 1, 0) + Count(board, row, col, player, -1, 0) - 1 >= 4
            || Count(board, row, col, player, 1, 1) + Count(board, row, col, player, -1, -1) - 1 >= 4
            || Count(board, row, col, player, 1, -1) + Count(board, row, col, player, -1, 1) - 1 >= 4;
    }

    private static int Count(int[,] board, int row, int col, int player, int dRow, int dCol)
    {
        var count = 0;
        var r = row;
        var c = col;

        while (r >= 0 && r < GameRoom.Rows && c >= 0 && c < GameRoom.Columns && board[r, c] == player)
        {
            count++;
            r += dRow;
            c += dCol;
        }

        return count;
    }
}
