import { Cell, type GameState, type PlayerSlot } from '../types/game'

export const ROWS = 6
export const COLS = 7

type GameBoardProps = {
  game: GameState
  seat: PlayerSlot
  onDrop: (column: number) => void
  disabled?: boolean
}

function cellAt(board: number[], row: number, col: number): number {
  return board[row * COLS + col] ?? Cell.Empty
}

function columnFull(board: number[], col: number): boolean {
  return cellAt(board, ROWS - 1, col) !== Cell.Empty
}

function statusMessage(
  game: GameState,
  slot: PlayerSlot,
): { title: string; detail: string } {
  switch (game.status) {
    case 'WaitingForPlayers': {
      const waitingFor = !game.hostConnected
        ? game.hostName
        : !game.guestConnected
          ? game.guestName
          : 'opponent'
      return {
        title: 'Waiting for opponent',
        detail: `${waitingFor} disconnected. Game pauses until they rejoin.`,
      }
    }
    case 'HostWon':
      return {
        title: slot === 'Host' ? 'You win!' : `${game.hostName} wins`,
        detail: 'Four in a row for the host.',
      }
    case 'GuestWon':
      return {
        title: slot === 'Guest' ? 'You win!' : `${game.guestName} wins`,
        detail: 'Four in a row for the guest.',
      }
    case 'Draw':
      return {
        title: 'Draw',
        detail: 'The board is full — no winner.',
      }
    case 'InProgress': {
      const myTurn = game.currentTurn === slot
      const turnName =
        game.currentTurn === 'Host' ? game.hostName : game.guestName
      return {
        title: myTurn ? 'Your turn' : `${turnName}'s turn`,
        detail: myTurn
          ? 'Click a column to drop a disc.'
          : 'Waiting for the other player…',
      }
    }
    default:
      return { title: 'Game', detail: '' }
  }
}

export function GameBoard({
  game,
  seat,
  onDrop,
  disabled = false,
}: GameBoardProps) {
  const { title, detail } = statusMessage(game, seat)
  const canPlay =
    !disabled &&
    game.status === 'InProgress' &&
    game.currentTurn === seat

  // Display top row first; storage has row 0 at the bottom.
  const displayRows = Array.from({ length: ROWS }, (_, i) => ROWS - 1 - i)

  return (
    <div className="board-wrap">
      <header className="board-status" aria-live="polite">
        <h1>{title}</h1>
        <p className="lede">{detail}</p>
      </header>

      <div className="players">
        <span
          className={`player-chip player-chip--host${
            game.currentTurn === 'Host' && game.status === 'InProgress'
              ? ' player-chip--active'
              : ''
          }`}
        >
          <span className="disc disc--host disc--sm" />
          {game.hostName}
          {seat === 'Host' ? ' (you)' : ''}
        </span>
        <span
          className={`player-chip player-chip--guest${
            game.currentTurn === 'Guest' && game.status === 'InProgress'
              ? ' player-chip--active'
              : ''
          }`}
        >
          <span className="disc disc--guest disc--sm" />
          {game.guestName}
          {seat === 'Guest' ? ' (you)' : ''}
        </span>
      </div>

      <div
        className="board"
        role="grid"
        aria-label="Four in a Row board"
        aria-rowcount={ROWS}
        aria-colcount={COLS}
      >
        {Array.from({ length: COLS }, (_, col) => {
          const full = columnFull(game.board, col)
          const interactive = canPlay && !full
          return (
            <button
              key={col}
              type="button"
              className={`board-col${interactive ? '' : ' board-col--idle'}`}
              disabled={!interactive}
              aria-label={`Drop in column ${col + 1}`}
              onClick={() => onDrop(col)}
            >
              {displayRows.map((row) => {
                const value = cellAt(game.board, row, col)
                const discClass =
                  value === Cell.Host
                    ? 'disc disc--host'
                    : value === Cell.Guest
                      ? 'disc disc--guest'
                      : 'disc disc--empty'
                return (
                  <span
                    key={row}
                    className="cell"
                    role="gridcell"
                    data-row={row}
                    data-col={col}
                  >
                    <span className={discClass} />
                  </span>
                )
              })}
            </button>
          )
        })}
      </div>
    </div>
  )
}
