namespace FourInRow.Server.Models;

public enum GameStatus
{
    WaitingForPlayers,
    InProgress,
    HostWon,
    GuestWon,
    Draw
}
