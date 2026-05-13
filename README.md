# Orleans Learning Lab — Chat

This repository is a hands-on learning project for Microsoft Orleans.

It implements a simple distributed chat + notifications system to practice:
- Grains (virtual actors) and grain interfaces
- Clustering + membership in MongoDB
- Grain state persistence in MongoDB
- Streams (used for live room messages)
- Reminders (used for notifications)
- Multi-silo setup + Orleans Dashboard
- A small API gateway (REST + SSE, plus gRPC)
- A minimal Next.js + Tailwind UI

## Suggested repository names

If you want a clearer “learning repo” name:
- `orleans-learning-lab`
- `orleans-learning-chat`
- `orleans-chat-lab`

## What runs where

**Backend (Docker)**
- 2 silos (Orleans cluster)
- 1 API gateway (`/api/*`, `/health`)
- 1 MongoDB (membership + reminders + grain storage + message history)

**Frontend (Node)**
- Next.js UI (messages + notifications)
- Uses SSE for “Live Messages”, with a polling fallback
- Proxies `/api/*` through the Next dev server so it works over LAN IP

## Ports

- UI: `http://localhost:3000` (or `http://<your-ip>:3000`)
- API (REST/SSE): `http://localhost:5000`
- API (gRPC): `http://localhost:5001`
- Dashboard: `http://localhost:8082` and `http://localhost:8083`
- MongoDB: `mongodb://localhost:27017`

## Run backend (Docker)

```bash
cd OrleansChat/docker
docker compose up --build -d
```

Health check:
- `http://localhost:5000/health`

## Run UI (Node)

```bash
cd OrleansChat/ui/orleanschat-web
npm ci
npm run dev -- -H 0.0.0.0 -p 3000
```

Open:
- `http://localhost:3000/`
- `http://<your-ip>:3000/`

## How “Live Messages” works

- Backend exposes SSE endpoint: `GET /api/rooms/{roomId}/stream`
- UI uses `EventSource(...)` to subscribe
- Because SSE can be unreliable when reverse-proxied, the UI also polls room history every few seconds as a fallback

## Quick troubleshooting

- UI stuck on “Connecting…”:
  - Open `http://<your-ip>:3000/health` (should return `{"status":"ok"}`)
  - If that works but messages don’t load, open DevTools → Console and share the error text
- Can’t open from another device:
  - Allow Windows Firewall inbound for ports `3000` (UI) and `5000` (API), or just use the UI proxy (`:3000`) only
