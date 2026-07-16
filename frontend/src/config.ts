/** Backend origin. Override with VITE_API_BASE_URL in `.env`. */
export const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL?.replace(/\/$/, '') ?? 'http://localhost:5275'

export const HUB_URL = `${API_BASE_URL}/hubs/game`

export const BOARD_ROWS = 6
export const BOARD_COLUMNS = 7
