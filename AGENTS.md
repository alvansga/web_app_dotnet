# CodeName Web App - AI Agent Playbook & Onboarding Guide

Welcome! This document is designed for AI Coding Agents (such as Antigravity, Cursor, and custom subagents) working on this repository. It provides key architectural rules, code style constraints, database management instructions, and workflows to ensure your changes are safe, consistent, and clean.

---

## 🧭 Codebase Context at a Glance

* **Backend**: .NET 9.0 ASP.NET Core Web API.
* **Database**: SQLite locally configured as `codename.db` in the project root.
* **ORM**: Entity Framework Core 9.0.0.
* **Frontend**: Single-Page Application (SPA) served statically via `wwwroot` using Vanilla HTML, CSS, and JS (no build tools, frameworks, or TailwindCSS unless explicitly requested).
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
* **Rule**: Do not add frontend frameworks (React, Vue, etc.) or packaging build tools (Webpack, Vite) unless requested. Keep the JS code in [script.js](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/wwwroot/script.js) clean, modular, and performant.
* **State Polling**: The frontend SPA polls `GET /api/rooms/{code}?playerId={playerId}` every 2 seconds to synchronize UI state. If you add new data to the game, ensure it's mapped to `RoomDetailResponse` and is properly bound in the UI inside the polling handler.
* **Styles**: Keep styling in [style.css](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/wwwroot/style.css). Maintain the dark-themed glassmorphism and clean flex/grid structures.

---

## 📝 Common Agent Workflows

### How to Add a Game Feature (e.g., "Add Timer Option")

1. **Domain Update**: Add properties like `TimerLimitSeconds` or `TimerStartedAt` to [GameRoom.cs](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/Domain/GameRoom.cs). Update state machines or checks.
2. **DTO Update**: Update `RoomDetailResponse` or create request DTOs inside `DTO/` for timer adjustments.
3. **Infrastructure Map**: If database storage is required, mapping the new properties in `AppDbContext.cs` OnModelCreating and running migration commands.
4. **Service Use-Case**: Add or update services in `Application/Services` to handle resetting/starting the timer. Register the service in `Program.cs`.
5. **Controller Route**: Expose endpoints on `GameRoomController.cs` matching REST standards.
6. **Frontend Integration**: Bind the new endpoint calls and render UI changes inside [index.html](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/wwwroot/index.html) and [script.js](file:///c:/Users/alva/MindPalace/Programming/csharp_projects/web_app/WebAppSandbox/wwwroot/script.js).
