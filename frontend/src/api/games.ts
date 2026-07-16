import { API_BASE_URL } from '../config'
import type { CreateGameRequest, CreateGameResponse, GameState } from '../types/game'

async function readError(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string }
    if (body.error) return body.error
  } catch {
    // ignore non-JSON bodies
  }
  return `Request failed (${response.status})`
}

function apiUrl(path: string): string {
  return API_BASE_URL ? `${API_BASE_URL}${path}` : path
}

export async function checkHealth(): Promise<boolean> {
  const response = await fetch(apiUrl('/health'))
  return response.ok
}

export async function createGame(
  request: CreateGameRequest,
): Promise<CreateGameResponse> {
  const response = await fetch(apiUrl('/api/games'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return (await response.json()) as CreateGameResponse
}

export async function getGame(gameId: string): Promise<GameState> {
  const response = await fetch(apiUrl(`/api/games/${encodeURIComponent(gameId)}`))

  if (response.status === 404) {
    throw new Error('Game not found.')
  }

  if (!response.ok) {
    throw new Error(await readError(response))
  }

  return (await response.json()) as GameState
}
