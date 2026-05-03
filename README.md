# Nordeus Challenge

Turn-based RPG built for the Nordeus Full Stack Challenge. Runs on a Spring Boot backend and uses Unity as frontend. Features a Drools-powered AI engine for enemy decision-making.

---

## Running the backend

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Java 24 + Gradle (only for dev mode)

### Release

```bash
cd backend
docker compose --profile full up --build
```

Builds the app and starts the backend.  
API available at `http://localhost:8080/api`.

To stop: `docker compose --profile full down`

### Option B — Dev mode

```bash
cd backend
./gradlew bootRun
```

---
## Running the frontend

After the backend is running, go to ```/UnityBuild/NordeusChallenge/``` and run the ```Nordeus-Challenge.exe``` (with the star icon)


## Backend API overview

All routes are prefixed with `/api`.

### Runs
| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/runs` | Start a new run (body: `{ "classId": "..." }`) |
| `GET` | `/runs/{runId}` | Get run state and map |
| `GET` | `/runs/{runId}/config` | Get run config (classes, moves, etc.) |
| `POST` | `/runs/{runId}/nodes/{nodeId}/start` | Enter a combat or boss node |
| `POST` | `/runs/{runId}/nodes/{nodeId}/enter` | Enter a shop or rest site |
| `POST` | `/runs/{runId}/shop/buy/{offerId}` | Buy an item from the current shop |
| `POST` | `/runs/{runId}/shop/sell/{itemId}` | Sell an inventory item |

### Player
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/runs/{runId}/player` | Get player state |
| `PUT` | `/runs/{runId}/player/moves` | Equip moves (body: `{ "moveIds": [...] }`) |
| `PUT` | `/runs/{runId}/player/equipment` | Equip items |
| `POST` | `/runs/{runId}/player/level-up` | Distribute stat points |

### Combat
| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/combats/{combatId}` | Get combat state |
| `POST` | `/combats/{combatId}/actions` | Submit player action (body: `{ "moveId": "..." }`) |
| `POST` | `/combats/{combatId}/pass` | Pass player turn |
| `GET` | `/combats/{combatId}/enemy-turn` | Trigger enemy turn |

---