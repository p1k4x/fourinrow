/** Path for a player opening / rejoining a game via seat join token. */
export function gamePath(gameId: string, joinToken: string): string {
  const params = new URLSearchParams({ token: joinToken })
  return `/g/${encodeURIComponent(gameId)}?${params.toString()}`
}

/** Absolute invite URL suitable for sharing / clipboard. */
export function inviteUrl(gameId: string, joinToken: string): string {
  return `${window.location.origin}${gamePath(gameId, joinToken)}`
}

export function parseGameRoute(
  pathname: string,
  search: string,
): { gameId: string; joinToken: string } | null {
  const match = pathname.match(/^\/g\/([^/]+)\/?$/)
  if (!match) return null

  const gameId = decodeURIComponent(match[1])
  const joinToken = new URLSearchParams(search).get('token')?.trim() ?? ''
  if (!gameId) return null

  return { gameId, joinToken }
}
