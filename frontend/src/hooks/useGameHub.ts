import { useCallback, useEffect, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { createGameHub, dropDisc as hubDropDisc, joinGame, startHub } from '../api/hub'
import type { GameState, JoinGameResult, PlayerSlot } from '../types/game'

export type HubStatus =
  | 'idle'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

export type UseGameHubResult = {
  game: GameState | null
  seat: PlayerSlot
  peerJoinToken: string | null
  status: HubStatus
  error: string | null
  dropDisc: (column: number) => Promise<void>
  clearError: () => void
}

/**
 * Connect to `/hubs/game`, call `JoinGame` with a seat token, and keep the seat across reconnects.
 * Disabled until `enabled` is true (e.g. after a non-empty join token is known).
 */
export function useGameHub(
  gameId: string,
  joinToken: string,
  enabled: boolean,
): UseGameHubResult {
  const [game, setGame] = useState<GameState | null>(null)
  const [seat, setSeat] = useState<PlayerSlot>('None')
  const [peerJoinToken, setPeerJoinToken] = useState<string | null>(null)
  const [status, setStatus] = useState<HubStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    if (!enabled || !gameId || !joinToken) {
      setStatus('idle')
      return
    }

    let cancelled = false

    const applyJoin = (result: JoinGameResult | null) => {
      if (!result || cancelled) return
      setSeat(result.seat)
      setPeerJoinToken(result.peerJoinToken)
    }

    const connection = createGameHub({
      onGameUpdated: (state) => {
        if (cancelled) return
        setGame(state)
        setError(null)
      },
      onError: (message) => {
        if (!cancelled) setError(message)
      },
      onReconnecting: () => {
        if (!cancelled) setStatus('reconnecting')
      },
      onReconnected: () => {
        if (cancelled) return
        setStatus('connected')
        // New connection id — re-claim the seat.
        void joinGame(connection, gameId, joinToken)
          .then(applyJoin)
          .catch((err: unknown) => {
            if (!cancelled) {
              setError(err instanceof Error ? err.message : 'Could not rejoin.')
            }
          })
      },
      onClose: () => {
        if (!cancelled) setStatus('disconnected')
      },
    })

    connectionRef.current = connection
    setStatus('connecting')
    setError(null)
    setGame(null)
    setSeat('None')
    setPeerJoinToken(null)

    void (async () => {
      try {
        await startHub(connection)
        if (cancelled) return
        const result = await joinGame(connection, gameId, joinToken)
        if (cancelled) return
        if (!result) {
          setStatus('disconnected')
          return
        }
        applyJoin(result)
        setStatus('connected')
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Could not connect.')
          setStatus('disconnected')
        }
      }
    })()

    return () => {
      cancelled = true
      connectionRef.current = null
      void connection.stop()
    }
  }, [gameId, joinToken, enabled])

  const dropDisc = useCallback(async (column: number) => {
    const connection = connectionRef.current
    if (!connection) {
      setError('Not connected.')
      return
    }
    try {
      await hubDropDisc(connection, gameId, column)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not drop disc.')
    }
  }, [gameId])

  const clearError = useCallback(() => setError(null), [])

  return { game, seat, peerJoinToken, status, error, dropDisc, clearError }
}
