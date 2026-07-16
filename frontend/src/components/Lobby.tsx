import { useState } from 'react'
import type { FormEvent } from 'react'
import { createGame } from '../api/games'

type LobbyProps = {
  onCreated: (gameId: string, hostName: string, guestName: string) => void
}

export function Lobby({ onCreated }: LobbyProps) {
  const [hostName, setHostName] = useState('')
  const [guestName, setGuestName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)

    const host = hostName.trim()
    const guest = guestName.trim()

    if (!host || !guest) {
      setError('Enter both names.')
      return
    }

    if (host.toLowerCase() === guest.toLowerCase()) {
      setError('Host and guest names must be different.')
      return
    }

    setSubmitting(true)
    try {
      const game = await createGame({ hostName: host, guestName: guest })
      onCreated(game.gameId, game.hostName, game.guestName)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create game.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="lobby">
      <h1>Start a match</h1>
      <p className="lede">
        Enter your name and your opponent&rsquo;s. You&rsquo;ll get a link to share —
        they open it to join.
      </p>

      <form className="lobby-form" onSubmit={handleSubmit}>
        <label className="field">
          <span>Your name</span>
          <input
            name="hostName"
            autoComplete="nickname"
            maxLength={40}
            value={hostName}
            onChange={(e) => setHostName(e.target.value)}
            placeholder="Alice"
            disabled={submitting}
            required
          />
        </label>

        <label className="field">
          <span>Opponent&rsquo;s name</span>
          <input
            name="guestName"
            autoComplete="off"
            maxLength={40}
            value={guestName}
            onChange={(e) => setGuestName(e.target.value)}
            placeholder="Bob"
            disabled={submitting}
            required
          />
        </label>

        {error && <p className="form-error" role="alert">{error}</p>}

        <button type="submit" className="btn-primary" disabled={submitting}>
          {submitting ? 'Creating…' : 'Create invite'}
        </button>
      </form>
    </section>
  )
}
