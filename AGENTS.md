# CodeName Web App - AI Agent Playbook & Onboarding Guide

Welcome! This document is designed for AI Coding Agents (such as Antigravity, Cursor, and custom subagents) working on this repository. It provides key architectural rules, code style constraints, database management instructions, and workflows to ensure your changes are safe, consistent, and clean.

---

## 🧭 Codebase Context at a Glance

* **Backend**: .NET 9.0 ASP.NET Core Web API.
* **Database**: SQLite locally configured as `codename.db` in the project root.
* **ORM**: Entity Framework Core 9.0.0.
* **Frontend**: Vue 3 Single-Page Application (SPA) served statically via `wwwroot`. Vue is loaded via CDN (`unpkg.com`) — no npm, no build tools, no `.vue` files. Components are plain JS objects with inline templates.
* **Game Assets**: Word lists are loaded from [words-id.txt](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/words-id.txt) (Indonesian wordlist) or fall back to English defaults defined in [StartGameService.cs](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/Application/Services/StartGameService.cs).

---

## 🛠️ Environment Setup & Tooling

To execute commands in the sandbox terminal:

### EF Core CLI Commands
Ensure `dotnet-ef` is installed globally:
```powershell
dotnet tool install --global dotnet-ef
```

To create a new migration after modifying model schemas:
```powershell
dotnet ef migrations add <MigrationName>
```

To apply migrations to the local SQLite database:
```powershell
dotnet ef database update
```

To reset the database completely:
```powershell
del codename.db
dotnet ef database update
```

### Running the App
Run the development server locally:
```powershell
dotnet run
```
Once running, the application serves static files (the frontend SPA) on `http://localhost:<port>/index.html` and Swagger API documentation at `http://localhost:<port>/swagger/index.html`.

---

## 🎯 Architecture & Coding Guidelines

This project uses a DDD-inspired Clean Architecture. Follow these instructions when creating or modifying code:

### 1. Rich Domain Models vs. Anemic Domain
* **Rule**: Keep domain logic inside domain models, not in Application Services or Controllers.
* **Entity Location**: Modifying game state rules, shuffling cards, setting colors, and evaluating win/loss conditions must be written inside [GameRoom.cs](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/Domain/GameRoom.cs) or [Player.cs](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/Domain/Player.cs).
* **Application Services**: Services inside `Application/Services` are orchestration layers. They retrieve entities from repositories, invoke the domain methods on those entities, and save changes back. Keep services focused on a single use case (e.g. `JoinRoomService`, `RevealCardService`).

### 2. Dependency Injection
* **Rule**: Register every new Application Service or Repository implementation in [Program.cs](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/Program.cs).
* **Scope**: Almost all services and repositories should be registered as `Scoped` (e.g. `builder.Services.AddScoped<MyNewService>();`).

### 3. Database Operations & Repositories
* **Rule**: Do not inject `AppDbContext` directly into Controllers or Services.
* **Practice**: Always use repository interfaces (e.g. `IGameRoomRepository`, `IPlayerRepository`) to interact with database records. If you add methods to repositories, update both the interface in `Application/Interfaces/` and the concrete implementation in `Infrastructure/Repositories/`.

### 4. API Controllers & DTOs
* **Rule**: Controllers must only handle routing, deserializing requests into DTOs, invoking services, and returning appropriate HTTP responses.
* **DTO Schema**: Ensure request bodies and response types use the classes located under `DTO/`. Never expose raw domain entities directly through the API endpoints.

### 5. Frontend SPA Integrity
* **Rule**: The frontend uses Vue 3 via CDN. Do NOT introduce npm, Webpack, Vite, `.vue` Single File Components, or other frameworks (React, Svelte, etc.).
* **Architecture**: The app is a reactive SPA with four page components driven by a `Vue.reactive()` store:
  * `wwwroot/js/store.js` — Reactive store (`store.player`, `store.room`, `store.myRole`, `store.page`). Single source of truth.
  * `wwwroot/js/api.js` — Centralized API layer. All fetch calls go here, returning parsed JSON or throwing errors.
  * `wwwroot/js/app.js` — Vue 3 app entry. Registers components, renders `<component :is="currentPageComponent">` based on `store.page`.
  * `wwwroot/js/components/` — Four page components: `WelcomePage.js`, `LobbyPage.js`, `WaitingRoomPage.js`, `GameBoardPage.js`.
* **State Polling**: Each page component manages its own polling via `setInterval` in `mounted()` / `clearInterval` in `beforeUnmount()`:
  * LobbyPage: polls `GET /api/rooms` every 3s for available rooms
  * WaitingRoomPage: polls `GET /api/rooms/{code}?playerId={playerId}` every 2s
  * GameBoardPage: polls `GET /api/rooms/{code}?playerId={playerId}` every 2.5s
* **Reactivity Rules**:
  * Always mutate `store.*` properties, never reassign `store` itself.
  * Use `store.room.xxx` in templates via `$store.room.xxx` shorthand (registered as `app.config.globalProperties.$store`).
  * Helper functions (`resetRoom()`, `syncMyRole()`, `isGameOver()`) are globals from `store.js` — use them directly.
* **Styles**: Keep all styling in [style.css](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/wwwroot/style.css). Maintain the dark-themed glassmorphism and clean flex/grid structures. CSS classes cover: card roles, spymaster hints, role badges, modals, game-over overlays, phase badges, and responsive breakpoints.

---

## 📝 Common Agent Workflows

### How to Add a Game Feature (e.g., "Add Timer Option")

1. **Domain Update**: Add properties like `TimerLimitSeconds` or `TimerStartedAt` to [GameRoom.cs](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/Domain/GameRoom.cs). Update state machines or checks.
2. **DTO Update**: Update `RoomDetailResponse` or create request DTOs inside `DTO/` for timer adjustments.
3. **Infrastructure Map**: If database storage is required, mapping the new properties in `AppDbContext.cs` OnModelCreating and running migration commands.
4. **Service Use-Case**: Add or update services in `Application/Services` to handle resetting/starting the timer. Register the service in `Program.cs`.
5. **Controller Route**: Expose endpoints on `GameRoomController.cs` matching REST standards.
6. **Frontend Integration**:
   * Add the API call to `wwwroot/js/api.js` following the existing pattern (async, throws on error, returns JSON).
   * If the feature affects room data, the polling in `GameBoardPage.js` will automatically pick it up from `RoomDetailResponse`. If it's a new action, add the method to the relevant page component.
   * Bind new UI elements in the appropriate component template and component data/methods.
   * If new state is needed, add reactive properties to the store in `wwwroot/js/store.js`.
