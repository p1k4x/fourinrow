import { useCallback, useEffect, useState } from 'react'
import { Lobby } from './components/Lobby'
import { WaitingRoom } from './components/WaitingRoom'
import { gamePath, parseGameRoute } from './lib/invite'
import './App.css'

type Route =
  | { kind: 'lobby' }
  | { kind: 'game'; gameId: string; joinToken: string }

function readRoute(): Route {
  const parsed = parseGameRoute(window.location.pathname, window.location.search)
  if (parsed) {
    return { kind: 'game', gameId: parsed.gameId, joinToken: parsed.joinToken }
  }
  return { kind: 'lobby' }
}

function navigate(path: string) {
  window.history.pushState({}, '', path)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

export default function App() {
  const [route, setRoute] = useState<Route>(readRoute)

  useEffect(() => {
    const sync = () => setRoute(readRoute())
    window.addEventListener('popstate', sync)
    return () => window.removeEventListener('popstate', sync)
  }, [])

  const goLobby = useCallback(() => {
    navigate('/')
  }, [])

  const handleCreated = useCallback((gameId: string, hostJoinToken: string) => {
    navigate(gamePath(gameId, hostJoinToken))
  }, [])

  return (
    <main className="shell">
      <p className="brand">Four in a Row</p>
      {route.kind === 'lobby' ? (
        <Lobby onCreated={handleCreated} />
      ) : (
        <WaitingRoom
          gameId={route.gameId}
          joinToken={route.joinToken}
          onBack={goLobby}
        />
      )}
    </main>
  )
}
