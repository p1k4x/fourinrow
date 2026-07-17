# Four in a Row — frontend

Vite + React + TypeScript client for the ASP.NET SignalR backend.

```bash
npm install
npm run dev
```

Requires the backend on `http://localhost:5275` (see root `README.md`). Configure via `.env` / `.env.example`.

## Lobby + join + play

1. Open http://localhost:5173
2. Enter your name + opponent name → **Create invite**
3. Host lands on `/g/{gameId}?token={hostJoinToken}` — SignalR joins automatically; copy the guest link
4. Guest opens `/g/{gameId}?token={guestJoinToken}` — joins the same hub; both see seat status until connected
5. When both are connected the board appears — click a column on your turn to drop a disc
