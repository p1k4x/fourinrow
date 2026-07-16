/** Matches backend PlayerSlot / board cell values. */
export const Cell = {
  Empty: 0,
  Host: 1,
  Guest: 2,
} as const

export type CellValue = (typeof Cell)[keyof typeof Cell]

export type PlayerSlot = 'None' | 'Host' | 'Guest'

export type GameStatus =
  | 'WaitingForPlayers'
  | 'InProgress'
  | 'HostWon'
  | 'GuestWon'
  | 'Draw'

/** Snapshot pushed by SignalR `GameUpdated` and `GET /api/games/{id}`. */
export type GameState = {
  gameId: string
  hostName: string
  guestName: string
  hostConnected: boolean
  guestConnected: boolean
  /** Flattened 6×7, row-major, row 0 = bottom. Values: 0 empty, 1 host, 2 guest. */
  board: number[]
  currentTurn: PlayerSlot
  status: GameStatus
  winner: PlayerSlot | null
}

export type CreateGameRequest = {
  hostName: string
  guestName: string
}

export type CreateGameResponse = {
  gameId: string
  hostName: string
  guestName: string
}
