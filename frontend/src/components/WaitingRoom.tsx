import { useState } from 'react'
import { GameBoard } from './GameBoard'
import { useGameHub, type HubStatus } from '../hooks/useGameHub'
import { inviteUrl } from '../lib/invite'
import { Cell, type GameState } from '../types/game'

type WaitingRoomProps = {
  gameId: string
  playerName: string
  onBack: () => void
}

function statusLabel(status: HubStatus): string | null {
  switch (status) {
    case 'connecting':
      return 'Connecting…'
    case 'reconnecting':
      return 'Reconnecting…'
    case 'disconnected':
      return 'Disconnected'
    default:
      return null
  }
}

function SeatRow({
  label,
  name,
  connected,
  isYou,
}: {
  label: string
  name: string
  connected: boolean
  isYou: boolean
}) {
  return (
    <div className="seat">
      <span
        className={`seat-dot${connected ? ' seat-dot--on' : ''}`}
        aria-hidden
      />
      <div>
        <p className="seat-label">
          {label}
          {isYou ? ' (you)' : ''}
        </p>
        <p className="seat-name">{name}</p>
      </div>
      <span className="seat-state">{connected ? 'Connected' : 'Waiting'}</span>
    </div>
  )
}

/** Show the board once play has started (or both are seated and in progress). */
function shouldShowBoard(game: GameState): boolean {
  if (
    game.status === 'InProgress' ||
    game.status === 'HostWon' ||
    game.status === 'GuestWon' ||
    game.status === 'Draw'
  ) {
    return true
  }
  // Opponent left mid-game — keep the board visible while waiting.
  return (
    game.status === 'WaitingForPlayers' &&
    game.board.some((cell) => cell !== Cell.Empty)
  )
}

export function WaitingRoom({ gameId, playerName, onBack }: WaitingRoomProps) {
  const canJoin = playerName.trim().length > 0
  const { game, status, error, dropDisc } = useGameHub(
    gameId,
    playerName.trim(),
    canJoin,
  )
  const [copied, setCopied] = useState(false)

  if (!canJoin) {
    return (
      <section className="waiting">
        <h1>Missing name</h1>
        <p className="lede">
          Open the invite link that includes your name, or create a new game
          from the lobby.
        </p>
        <button type="button" className="btn-secondary" onClick={onBack}>
          Back to lobby
        </button>
      </section>
    )
  }

  const hubBanner = statusLabel(status)
  const showBoard = !!game && shouldShowBoard(game)
  const bothConnected =
    !!game && game.hostConnected && game.guestConnected
  const isHost =
    !!game && playerName.toLowerCase() === game.hostName.toLowerCase()
  const isGuest =
    !!game && playerName.toLowerCase() === game.guestName.toLowerCase()
  const guestInvite = game ? inviteUrl(game.gameId, game.guestName) : ''

  async function copyInvite() {
    if (!guestInvite) return
    try {
      await navigator.clipboard.writeText(guestInvite)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 2000)
    } catch {
      setCopied(false)
    }
  }

  if (showBoard && game) {
    return (
      <section className="play">
        {hubBanner && (
          <p className="hub-banner" role="status">
            {hubBanner}
          </p>
        )}
        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        <GameBoard
          game={game}
          playerName={playerName}
          onDrop={(column) => void dropDisc(column)}
          disabled={status !== 'connected'}
        />
        <p className="room-id">
          Room <code>{game.gameId}</code>
        </p>
        <button type="button" className="btn-secondary" onClick={onBack}>
          Leave
        </button>
      </section>
    )
  }

  return (
    <section className="waiting">
      <WaitingHeader
        game={game}
        bothConnected={bothConnected}
        isHost={isHost}
        isGuest={isGuest}
        status={status}
      />

      {hubBanner && (
        <p className="hub-banner" role="status">
          {hubBanner}
        </p>
      )}

      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}

      {game && (
        <>
          <div className="seats" aria-live="polite">
            <SeatRow
              label="Host"
              name={game.hostName}
              connected={game.hostConnected}
              isYou={isHost}
            />
            <SeatRow
              label="Guest"
              name={game.guestName}
              connected={game.guestConnected}
              isYou={isGuest}
            />
          </div>

          {isHost && !bothConnected && (
            <div className="invite-box">
              <label className="field">
                <span>Share with {game.guestName}</span>
                <input
                  className="invite-input"
                  readOnly
                  value={guestInvite}
                  onFocus={(e) => e.target.select()}
                />
              </label>
              <button type="button" className="btn-primary" onClick={copyInvite}>
                {copied ? 'Copied!' : 'Copy invite link'}
              </button>
            </div>
          )}

          <p className="room-id">
            Room <code>{game.gameId}</code>
          </p>
        </>
      )}

      {!game && status === 'connecting' && (
        <p className="lede">Joining room {gameId}…</p>
      )}

      <button type="button" className="btn-secondary" onClick={onBack}>
        Leave
      </button>
    </section>
  )
}

function WaitingHeader({
  game,
  bothConnected,
  isHost,
  isGuest,
  status,
}: {
  game: GameState | null
  bothConnected: boolean
  isHost: boolean
  isGuest: boolean
  status: HubStatus
}) {
  if (bothConnected) {
    return (
      <>
        <h1>Both connected</h1>
        <p className="lede">
          {game!.hostName} and {game!.guestName} are in. Starting…
        </p>
      </>
    )
  }

  if (game && (isHost || isGuest)) {
    const waitingFor = isHost
      ? game.guestConnected
        ? null
        : game.guestName
      : game.hostConnected
        ? null
        : game.hostName

    return (
      <>
        <h1>{isHost ? 'Waiting for opponent' : "You're in"}</h1>
        <p className="lede">
          {waitingFor
            ? `Connected as ${isHost ? 'host' : 'guest'}. Waiting for ${waitingFor} to open their link.`
            : 'Connected. Waiting for both seats…'}
        </p>
      </>
    )
  }

  if (status === 'disconnected') {
    return (
      <>
        <h1>Could not join</h1>
        <p className="lede">Check the error below, then try again from the lobby.</p>
      </>
    )
  }

  return (
    <>
      <h1>Joining…</h1>
      <p className="lede">Connecting to the game hub.</p>
    </>
  )
}
