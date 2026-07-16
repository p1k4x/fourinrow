/** Path for a player opening / rejoining a game. */
export function gamePath(gameId: string, playerName: string): string {
  const params = new URLSearchParams({ name: playerName })
  return `/g/${encodeURIComponent(gameId)}?${params.toString()}`
}

/** Absolute invite URL suitable for sharing / clipboard. */
export function inviteUrl(gameId: string, playerName: string): string {
  return `${window.location.origin}${gamePath(gameId, playerName)}`
}

export function parseGameRoute(
  pathname: string,
  search: string,
): { gameId: string; playerName: string } | null {
  const match = pathname.match(/^\/g\/([^/]+)\/?$/)
  if (!match) return null

  const gameId = decodeURIComponent(match[1])
  const playerName = new URLSearchParams(search).get('name')?.trim() ?? ''
  if (!gameId) return null

  return { gameId, playerName }
}
