# Four in a Row

Real-time two-player Four in a Row with invite links. No database — game rooms live in memory on the server.

**Tracking:** [FOUR Jira board](https://pikachurro.atlassian.net/jira/software/projects/FOUR/boards)

## Play locally

You need two terminals: one for the API, one for the UI.

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) and [Node.js](https://nodejs.org/) (for `npm`).

### 1. Start the backend

```bash
export PATH="$HOME/.dotnet:$PATH"   # if dotnet is not already on PATH
cd backend
dotnet run
```

API + SignalR hub: **http://localhost:5275**

### 2. Start the frontend

```bash
cd frontend
cp .env.example .env   # first time only; points at http://localhost:5275
npm install
npm run dev
```

App: **http://localhost:5173**

### 3. Play with two browsers

Each seat needs its own browser session (two windows of the same browser will share storage and fight over one connection). Use either:

- two different browsers (e.g. Chrome + Firefox), or
- one normal window and one private/incognito window, or
- two browser profiles

**Host**

1. Open http://localhost:5173
2. Enter your name and the guest’s name → create room
3. Stay on the waiting room; copy the guest invite link

**Guest**

1. Paste the invite link in the second browser / profile
2. You land on `/g/{gameId}?token={guestJoinToken}` and join via SignalR

When both seats show connected, the 6×7 board appears. Click a column to drop a disc. Turns, wins, and draws update live.

Rooms are in-memory only — restarting the backend clears all games.

---

## Run with Docker

One command starts both services: ASP.NET Core API + nginx (static SPA + reverse proxy).

**Prerequisites:** [Docker](https://docs.docker.com/get-docker/) with Compose.

```bash
docker compose up --build
```

App: **http://localhost:8080**

nginx serves the React build and proxies `/api`, `/hubs`, and `/health` to the backend (WebSockets enabled for SignalR). The SPA is built with an empty `VITE_API_BASE_URL`, so the browser stays same-origin.

Stop with `Ctrl+C`, or run detached with `docker compose up --build -d` and stop with `docker compose down`.

Local `dotnet run` / `npm run dev` (above) is unchanged — use Compose when you want the packaged stack.

### Deploy (VPS / any Docker host)

The same Compose stack is what you run on a server: clone, build, detach.

```bash
git clone https://github.com/p1k4x/fourinrow.git
cd fourinrow
docker compose up --build -d
```

App: `http://<server-ip>:8080`

Update later:

```bash
git pull
docker compose up --build -d
```

Stop with `docker compose down`.

**Production notes**

- **HTTPS** — Compose serves HTTP only. Put TLS in front (Caddy, Traefik, Cloudflare, or host nginx) and proxy to `127.0.0.1:8080`. Keep WebSocket upgrades for `/hubs/` (already set in `frontend/nginx.conf`).
- **One instance** — rooms are in-memory. Don’t scale `backend` to multiple replicas without sticky sessions + shared state; a restart/redeploy clears games.
- **Firewall** — open 80/443 (or 8080 if you expose Compose directly). You don’t need to publish the backend port.
- **Health** — `GET /health` via the frontend proxy.

Example TLS with Caddy on the host (Compose unchanged; Caddy terminates TLS and forwards to port 8080):

```caddy
play.example.com {
    reverse_proxy 127.0.0.1:8080
}
```

---

## What is SignalR?

**SignalR** is ASP.NET’s real-time library. Clients keep a persistent connection to the server (usually **WebSockets**). Either side can push messages instantly — perfect for turns, board updates, and “opponent joined” events.

Compared to a normal Web API:

| HTTP API | SignalR |
|----------|---------|
| Client asks, server answers | Server can push anytime |
| Good for create-game | Good for live play |
| One request / response | Ongoing connection |

In this project:

1. **HTTP** `POST /api/games` — create a room + invite id  
2. **SignalR** `/hubs/game` — join, drop discs, receive `GameUpdated`

## Backend (`backend/`)

ASP.NET Core 10 + SignalR.

### Run

```bash
export PATH="$HOME/.dotnet:$PATH"
cd backend
dotnet run
```

Server listens on **http://localhost:5275**.

### HTTP

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/health` | Liveness |
| `POST` | `/api/games` | Create invite room |
| `GET` | `/api/games/{id}` | Snapshot of room state |

Create body:

```json
{ "hostName": "Alice", "guestName": "Bob" }
```

Create response includes `hostJoinToken` and `guestJoinToken` (opaque seat credentials). Invite links use `?token=…`; display names stay labels only.

### SignalR hub (`/hubs/game`)

| Client → server | Meaning |
|-----------------|---------|
| `JoinGame(gameId, joinToken)` | Sit as host or guest (opaque seat token) |
| `DropDisc(gameId, column)` | Drop in column `0`–`6` |

| Server → client | Meaning |
|-----------------|---------|
| `GameUpdated` | Full board + status DTO |
| `Error` | Validation / join failure |

Board is 6×7, flattened row-major, **row 0 = bottom**. Values: `0` empty, `1` host, `2` guest.

## Frontend (`frontend/`)

Vite + React + TypeScript with `@microsoft/signalr`.

### Run

```bash
cd frontend
npm install
npm run dev
```

App listens on **http://localhost:5173**. Point `VITE_API_BASE_URL` in `frontend/.env` at the backend (default `http://localhost:5275`). Leave it empty for same-origin (Vite proxy or Docker nginx). CORS on the backend already allows Vite ports; `vite.config.ts` also proxies `/api`, `/hubs`, and `/health`.

### Layout

| Path | Role |
|------|------|
| `src/config.ts` | API + hub URLs |
| `src/types/game.ts` | Shared DTOs |
| `src/api/games.ts` | HTTP create/get/health |
| `src/api/hub.ts` | SignalR join / drop helpers |
| `src/components/Lobby.tsx` | Host + guest names → `POST /api/games` |
| `src/components/WaitingRoom.tsx` | Invite URL → SignalR join / wait / play |
| `src/components/GameBoard.tsx` | 6×7 board + column drop |
| `src/hooks/useGameHub.ts` | Hub connect / rejoin / drop / `GameUpdated` |
| `src/lib/invite.ts` | Invite paths `/g/{id}?token=…` |

**Lobby:** enter your name and opponent’s → create room → host lands on `/g/{gameId}?token={hostJoinToken}` with a copyable guest invite link (guest token from `JoinGame`).

**Join:** opening `/g/{gameId}?token=…` connects to SignalR, calls `JoinGame`, shows seat status until both players are connected (reconnect re-claims the seat). Display names are labels only; seat access is the join token.

**Play:** once both are in, the 6×7 board appears — click a column to `DropDisc`; turn, win, draw, and wait-for-opponent states update live via `GameUpdated`.
