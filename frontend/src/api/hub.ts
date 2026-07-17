import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import { HUB_URL } from '../config'
import type { GameState, JoinGameResult } from '../types/game'

export type GameHubHandlers = {
  onGameUpdated?: (state: GameState) => void
  onError?: (message: string) => void
  onReconnecting?: () => void
  onReconnected?: () => void
  onClose?: (error?: Error) => void
}

/** Build a SignalR connection to `/hubs/game` (not started yet). */
export function createGameHub(handlers: GameHubHandlers = {}): HubConnection {
  const connection = new HubConnectionBuilder()
    .withUrl(HUB_URL)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build()

  connection.on('GameUpdated', (state: GameState) => {
    handlers.onGameUpdated?.(state)
  })

  connection.on('Error', (message: string) => {
    handlers.onError?.(message)
  })

  connection.onreconnecting(() => handlers.onReconnecting?.())
  connection.onreconnected(() => handlers.onReconnected?.())
  connection.onclose((error) => handlers.onClose?.(error))

  return connection
}

export async function startHub(connection: HubConnection): Promise<void> {
  if (connection.state === HubConnectionState.Disconnected) {
    await connection.start()
  }
}

export async function joinGame(
  connection: HubConnection,
  gameId: string,
  joinToken: string,
): Promise<JoinGameResult | null> {
  return (await connection.invoke('JoinGame', gameId, joinToken)) as JoinGameResult | null
}

export async function dropDisc(
  connection: HubConnection,
  gameId: string,
  column: number,
): Promise<void> {
  await connection.invoke('DropDisc', gameId, column)
}
