/**
 * Backend origin. Override with VITE_API_BASE_URL in `.env`.
 * Empty string = same-origin (Docker / nginx reverse-proxy).
 * Unset = local default http://localhost:5275.
 */
const rawBase = import.meta.env.VITE_API_BASE_URL
export const API_BASE_URL =
  rawBase === undefined
    ? 'http://localhost:5275'
    : String(rawBase).replace(/\/$/, '')

export const HUB_URL = API_BASE_URL ? `${API_BASE_URL}/hubs/game` : '/hubs/game'

export const BOARD_ROWS = 6
export const BOARD_COLUMNS = 7
