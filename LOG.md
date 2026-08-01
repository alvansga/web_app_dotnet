# Changelog

All notable changes to **CodeName Web App** are documented in this file.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).  
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [1.5.0] — Unreleased

### Added
- Online player traffic indicator in Game Lobby: a compact stats bar shows `🟢 N online • 🏠 N di lobby • 🎯 N bermain`
- Backend: `GET /api/rooms/stats` endpoint returning aggregated player counts (`TotalOnline`, `InLobby`, `Playing`, `TotalRooms`)
- Backend: `GetOnlineStatsService` — aggregates player counts by room status (Waiting → lobby, others → playing)
- Backend: `DTO/OnlineStatsResponse.cs` — response DTO for stats endpoint
- Frontend: `api.getOnlineStats()` in `api.js` — fetches stats from the new endpoint
- Frontend: `LobbyPage.js` — polls `/api/rooms/stats` every 5s and renders `.online-stats-bar`
- CSS: `.online-stats-bar`, `.stat-item`, `.stat-sep` — glassmorphism-styled traffic bar

---

## [1.4.0] — Unreleased

### Added
- Game-over board reveal: all 25 cards show their true colors (RedAgent, BlueAgent, Bystander, Assassin) when the game ends, giving players the "aha!" moment of seeing the full board
- Game-over banner replaces the full-screen modal overlay — board stays visible behind a compact winner announcement banner
- Backend: `GetRoomDetailService` exposes all `CardRole` values when `GamePhase == Ended` (previously only spymasters saw unrevealed roles)

### Changed
- `GameBoardPage.js` template: replaced `<div class="modal-overlay">` with `<div class="game-over-banner">` inline at top of `.game-container`
- `GameBoardPage.js` `cardClasses()`: added `showGameOver` branch that applies `red-agent`/`blue-agent`/`bystander`/`assassin` + `revealed` classes to all cards regardless of `isRevealed` state
- `style.css`: replaced section "*13. GAME OVER OVERLAY*" with "*13. GAME OVER BANNER*" — flexbox layout with icon+text left, button right; responsive stacking on mobile

### Removed
- Full-screen `.modal-overlay` + `.game-over-box` for game-over display (replaced by `.game-over-banner`)

---

## [1.3.0] — Unreleased

### Added
- Team-specific player roles: `RedSpymaster`, `BlueSpymaster`, `RedFieldOperative`, `BlueFieldOperative`
- `CurrentTurn` property to `GameState` — tracks which team's turn it is
- `SpymasterTeam` property to `Clue` — clues are tagged by the spymaster's team (Red/Blue)
- Role selection UI in `WaitingRoomPage` — players pick team and role before the game starts
- `store.myTeam` in the frontend store — derived from the player's role
- Team-based turn indicator on `GameBoardPage` (pulsing "🔴 Red's Turn" / "🔵 Blue's Turn" badge)
- Team-colored clue cards — Red team clues show red, Blue team clues show blue
- Turn-based restrictions — spymasters can only give clues on their team's turn, field operatives can only reveal cards on their team's turn
- `canManageClue`, `clueTeamClass`, `clueTeamTagClass` methods to `GameBoardPage` for team-aware clue management
- CSS: `.turn-indicator`, `.my-team-badge`, `.clue-team-red`/`.clue-team-blue`, `.clue-team-tag`, `.player-role-tag` variants, `.role-selection-section`, `.role-cards-grid`, `.player-dot`, `.player-role-badge`

### Changed
- `AssignRoleService` now handles team-specific role assignment endpoints
- `StartGameService` respects pre-assigned team roles instead of auto-assigning legacy roles
- `GetRoomDetailService` and `ClueService` updated for team-based clue storage
- `GameBoardPage`: removed role selection modal (moved to WaitingRoom), removed legacy role fallbacks (`'Spymaster'`, `'FieldOperative'`), fixed corrupted `cardClasses`/`loadBoard` code
- Role badge CSS split into team-specific variants (`.spymaster-red`, `.spymaster-blue`, `.operative-red`, `.operative-blue`)

### Removed
- Legacy role strings: `'Spymaster'`, `'FieldOperative'` — replaced by team-specific roles
- Role selection modal from `GameBoardPage` (`chooseRole`, `showRoleModal`, `roleMsg`, `choosing`)

---

## [1.2.0] — Unreleased

### Fixed
- "Back to Lobby" button on game-over screen now keeps players inside the room (navigates to waiting room instead of leaving the room entirely)

---

## [1.1.0] — Unreleased (staged, not committed)

### Added
- Vue 3 reactive frontend replacing vanilla JS (`wwwroot/script.js` deleted)
- `wwwroot/js/store.js` — Vue reactive store, single source of truth
- `wwwroot/js/api.js` — Centralized API layer for all 12 backend endpoints
- `wwwroot/js/app.js` — Vue 3 app entry with dynamic page routing via `<component :is>`
- `wwwroot/js/components/WelcomePage.js` — Player name input & game rules
- `wwwroot/js/components/LobbyPage.js` — Create/join rooms, available rooms list, logout
- `wwwroot/js/components/WaitingRoomPage.js` — Player list, start game, copy room code
- `wwwroot/js/components/GameBoardPage.js` — Card grid, role modal, clue system, game over overlay
- Reactive "Copy Room Code" feedback (was silently broken in vanilla JS)
- Inline error messages replacing `alert()` dialogs in GameBoard
- Room-not-found handling: redirect to lobby when room is deleted during polling
- `AGENTS.md` — AI agent playbook with Vue 3 architecture, reactivity rules, and workflow guide
- `DOCUMENTATION.md` — System documentation with Vue 3 frontend architecture section
- `LOG.md` — This changelog

### Changed
- `wwwroot/index.html` — minimal shell with `<div id="app">` mount point and Vue CDN scripts
- `wwwroot/style.css` — added styles for spymaster hints, role badges, modals, game-over overlays, phase badges

### Removed
- `wwwroot/script.js` — replaced by Vue 3 components

---

## [1.0.0] — 2026-07-19

_Commit: `6617252` — dont forget update db first_  
_Commit: `f88002f` — dotnet 10.0_

### Changed
- Upgraded target framework from .NET 9.0 to .NET 10.0

---

## [0.4.0] — 2026-04-19

_Commit: `2c3ebc6` — add agentsmd and documentationmd by antigravity_

### Added
- `AGENTS.md` — AI agent playbook & onboarding guide
- `DOCUMENTATION.md` — Full system documentation

---

## [0.3.0] — 2026-04-19

_Commit: `a3d1a71` — tambah binding gamecard dan clue ke gameroom id_

### Added
- GameCard ↔ GameRoom FK binding
- Clue ↔ GameRoom FK binding with cascade delete

### Fixed
- Field Operative role check bug — field operatives can now properly reveal cards (`73c0bbd`)
- Card grid responsive layout on Android devices — forced 5×5 grid (`ffec29f`)

### Changed
- Switched wordlist from English to Indonesian (`words-id.txt`) (`ef8b46e`)

---

## [0.2.0] — 2026-04-18

_Commit: `aa47479` — tambah api utk ngasih clue bagi spymaster_  
_Commit: `07770df` — add delete rooms api_  
_Commit: `f1802fe` — add words.txt, random word baca dari words.txt_  
_Commit: `7f82130` — playable, first client to click the start game is the spymaster_  
_Commit: `8720b66` — add api role spymaster/field operative, api action choose card_

### Added
- Clue system: Spymaster can add (`POST /api/rooms/{code}/clue`) and remove (`DELETE`) clues
- Delete room API endpoint (`DELETE /api/rooms/{code}`)
- Word list loading from `words.txt` file with random card distribution
- Spymaster / Field Operative role assignment APIs
- Card reveal action API (`POST /api/rooms/{code}/reveal/{cardId}`)
- First player to start the game is automatically assigned as Spymaster

---

## [0.1.0] — 2026-04-17

_Commit: `08508f7` — refactor to more HUMAN readable_  
_Commit: `918e17b` — simple frontend codename_  
_Commit: `3f45b60` — add card, add api room start_  
_Commit: `c13cbde` — tambah gamestatedto_  
_Commit: `1c23559` — add status state_  
_Commit: `9195592` — add api GET api/players_  
_Commit: `08a536a` — add get room details_  
_Commit: `41b0733` — add join room_

### Added
- Initial ASP.NET Core Web API project with SQLite + EF Core
- Domain models: `GameRoom`, `Player`, `CodeName`
- Game state tracking: phase, round, status, winner
- Player API: create player (`POST /api/players`), list all players (`GET /api/players`)
- Room API: create room (`POST /api/rooms`), get room detail (`GET /api/rooms/{code}`), join room (`POST /api/rooms/join`)
- Game start API (`POST /api/rooms/{code}/start`) with card initialization
- Basic vanilla HTML/CSS/JS frontend with four pages (Welcome, Lobby, Waiting Room, Game Board)
- Frontend state polling loop and local storage player persistence

---

## Version Summary

| Version | Date | Description |
|---------|------|-------------|
| [1.1.0] | Unreleased | Vue 3 frontend conversion, inline error messages, room-not-found handling |
| [1.0.0] | 2026-07-19 | .NET 10.0 upgrade |
| [0.4.0] | 2026-04-19 | AGENTS.md + DOCUMENTATION.md |
| [0.3.0] | 2026-04-19 | FK bindings, bug fixes, Indonesian wordlist |
| [0.2.0] | 2026-04-18 | Clue system, room deletion, role assignment, card reveal |
| [0.1.0] | 2026-04-17 | Initial project: API + basic frontend + game state |

---

## Git Tags

```
(no tags yet — start with v1.0.0)
```

To tag the current release:
```powershell
git tag v1.0.0 6617252
git push origin v1.0.0
```

To tag the Vue 3 conversion after committing:
```powershell
git tag v1.1.0
git push origin v1.1.0
```
