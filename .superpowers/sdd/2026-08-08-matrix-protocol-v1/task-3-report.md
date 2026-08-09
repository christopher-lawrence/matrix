# Task 3: JSON Command Dispatch

## Implemented

- Replaced slash-command parsing with `ClientMessage` JSON deserialization in `CommandHandler`.
- Dispatches commands by case-insensitive protocol message type: `look`, `who`, `move`, `say`, and `help`.
- Sends protocol `error` messages with the required exact values for invalid JSON, unknown commands, and command execution failures.
- Migrated `ICommand` and all command implementations to receive nullable JSON args and expose `Type` instead of slash-prefixed `Name`.
- Migrated command responses to `IProtocolMessageSender`: `roomState`, `who`, `chatMessage`, `userLeft`, `userEntered`, `help`, and `error` payloads.
- Validates `MoveArgs` and `SayArgs`; moving now returns destination `roomState` to the mover.
- Left onboarding and the console client unchanged, as they are assigned to Tasks 4 and 5.

## Build/Test Output

Command:

```text
dotnet build Matrix.Server/Matrix.Server.csproj
```

Result:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.64
```

`git diff --check` completed without findings. No test project exists, and none was added per the task instructions.

## Files Changed

- `Matrix.Server/Services/Commands.cs`
- `Matrix.Server/Services/CommandHandler.cs`
- `Matrix.Server/Services/Commands/LookCommand.cs`
- `Matrix.Server/Services/Commands/WhoCommand.cs`
- `Matrix.Server/Services/Commands/GoCommand.cs`
- `Matrix.Server/Services/Commands/SayCommand.cs`
- `Matrix.Server/Services/Commands/HelpCommand.cs`
- `.superpowers/sdd/2026-08-08-matrix-protocol-v1/task-3-report.md`

## Self-Review

- Confirmed command type constants are used without slash prefixes and help examples use the required JSON values.
- Confirmed command and dispatch output now flow through `IProtocolMessageSender`, which delegates socket writes to `IConnectionManager`.
- Confirmed slash parsing helpers and `ICommand.Name` references were removed from the migrated command surface.
- Confirmed room-state payload construction filters blank usernames and returns ordered users and lowercase ordered exits for both look and movement.
- Scoped changes to Task 3 command dispatch and command response behavior.

## Concerns

None identified for Task 3. Onboarding remains plaintext by design until Task 4.
