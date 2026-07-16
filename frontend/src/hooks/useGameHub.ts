import { useCallback, useEffect, useRef, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { createGameHub, dropDisc as hubDropDisc, joinGame, startHub } from '../api/hub'
import type { GameState } from '../types/game'

export type HubStatus =
  | 'idle'
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

export type UseGameHubResult = {
  game: GameState | null
  status: HubStatus
  error: string | null
  dropDisc: (column: number) => Promise<void>
  clearError: () => void
}

/**
 * Connect to `/hubs/game`, call `JoinGame`, and keep the seat across reconnects.
 * Disabled until `enabled` is true (e.g. after a non-empty player name is known).
 */
export function useGameHub(
  gameId: string,
  playerName: string,
  enabled: boolean,
): UseGameHubResult {
  const [game, setGame] = useState<GameState | null>(null)
  const [status, setStatus] = useState<HubStatus>('idle')
  const [error, setError] = useState<string | null>(null)
  const connectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    if (!enabled || !gameId || !playerName) {
      setStatus('idle')
      return
    }

    let cancelled = false

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
        void joinGame(connection, gameId, playerName).catch((err: unknown) => {
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

    void (async () => {
      try {
        await startHub(connection)
        if (cancelled) return
        await joinGame(connection, gameId, playerName)
        if (!cancelled) setStatus('connected')
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
  }, [gameId, playerName, enabled])

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

  return { game, status, error, dropDisc, clearError }
}
