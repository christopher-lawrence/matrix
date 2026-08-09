# Task 1: Shared Protocol Contract Report

## Implemented

Added the shared Matrix Protocol v1 contract under `Matrix.Core/Protocol`:

- `ClientMessage` and `ServerMessage` envelopes.
- `ProtocolMessageTypes` constants for all client commands and server events.
- `MoveArgs`, `SayArgs`, and `UsernameArgs` command payloads.
- `ErrorData`, `RoomStateData`, `ChatMessageData`, `UserPresenceData`, `PromptData`, `HelpData`, `HelpCommandData`, and `WhoData` server payloads.
- `ProtocolJson` with shared web-default JSON serializer options, client-message deserialization, argument deserialization, and server-message serialization.

No server, client, project, or runtime behavior was changed.

## Build/Test Output

Required command:

```text
dotnet build Matrix.Core/Matrix.Core.csproj
```

Result:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

No automated tests were run because the repository has no test project and the task explicitly forbids adding a test framework.

Additional verification:

```text
git diff --check
```

Result: clean.

## Files Changed

- `Matrix.Core/Protocol/ChatMessageData.cs`
- `Matrix.Core/Protocol/ClientMessage.cs`
- `Matrix.Core/Protocol/ErrorData.cs`
- `Matrix.Core/Protocol/HelpData.cs`
- `Matrix.Core/Protocol/MoveArgs.cs`
- `Matrix.Core/Protocol/PromptData.cs`
- `Matrix.Core/Protocol/ProtocolJson.cs`
- `Matrix.Core/Protocol/ProtocolMessageTypes.cs`
- `Matrix.Core/Protocol/RoomStateData.cs`
- `Matrix.Core/Protocol/SayArgs.cs`
- `Matrix.Core/Protocol/ServerMessage.cs`
- `Matrix.Core/Protocol/UserPresenceData.cs`
- `Matrix.Core/Protocol/UsernameArgs.cs`
- `Matrix.Core/Protocol/WhoData.cs`
- `.superpowers/sdd/2026-08-08-matrix-protocol-v1/task-1-report.md`

## Self-Review

- Confirmed all requested protocol files exist.
- Confirmed the constants and public record signatures match the task brief.
- Confirmed nullable reference types compile without warnings in the new files.
- Confirmed JSON handling uses `JsonSerializerDefaults.Web`, accepts nullable args, rejects invalid JSON and blank message types, and serializes server envelopes without indentation.
- Confirmed no server/client behavior or project configuration was modified.
- Confirmed whitespace validation is clean.

## Concerns

None identified.
