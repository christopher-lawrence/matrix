# Agent Instructions

## Repo Overview
- This is a .NET 10 solution for a small WebSocket-based text world.
- `Matrix.Core` contains domain types, strongly typed IDs, and the in-memory `WorldMap`.
- `Matrix.Server` is an ASP.NET Core WebSocket server exposing `GET /ws`.
- `Matrix.Client` is currently a placeholder console app.

## Development Commands
- Build the full solution with `dotnet build Matrix.slnx`.
- Run the server with `dotnet run --project Matrix.Server`.
- There are currently no test projects. Do not add a test framework unless the task specifically requires tests or an adjacent test project exists.

## Architecture Notes
- Server dependencies are registered in `Matrix.Server/Program.cs`; new commands must be registered as `ICommand` singletons.
- WebSocket connection lifecycle starts in `Matrix.Server/Controllers/WebSocketController.cs` and is handled by `WebSocketConnectionService`.
- Session state is managed through `ISessionManager`; avoid bypassing it by directly storing connection/session state elsewhere.
- Socket writes should go through `IConnectionManager` unless there is a lifecycle-specific reason to write directly to the socket.
- The world graph currently lives in `Matrix.Core/Services/WorldMap.cs`; keep movement/domain logic in core where practical.

## Coding Conventions
- Keep nullable reference types enabled and handle nulls explicitly.
- Use existing C# style: file-scoped namespaces, explicit interfaces for services, constructor injection, and `sealed` classes where inheritance is not intended.
- Prefer strongly typed IDs from `Matrix.Core/Ids` over raw `Guid` values in domain/core code.
- Keep command names slash-prefixed, e.g. `/look`, `/who`, `/go`.
- Make changes small and scoped; do not refactor unrelated lifecycle, session, or WebSocket behavior while adding features.

## Command Implementation Guidelines
- Implement commands under `Matrix.Server/Services/Commands`.
- Implement `ICommand` and return user-facing failures through `IConnectionManager.SendTextAsync`.
- Retrieve current session with `ISessionManager.TryGetByConnectionId`.
- Validate and trim command parameters before using them.
- Log operational failures with structured logging placeholders.

## Validation
- Run `dotnet build Matrix.slnx` after code changes when practical.
- If adding behavior without tests, manually exercise the server with a WebSocket client against `/ws`.
- Do not commit changes unless explicitly asked.
