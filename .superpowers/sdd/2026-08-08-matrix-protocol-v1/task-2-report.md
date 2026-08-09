# Task 2: Server Protocol Sending

## Implemented

- Added `IProtocolMessageSender` with `SendAsync` and `BroadcastAsync`.
- Added `ProtocolMessageSender`, which wraps `ServerMessage` creation and `ProtocolJson.Serialize` before delegating socket writes to `IConnectionManager`.
- Registered `IProtocolMessageSender` as a singleton beside the existing `IConnectionManager` registration.
- Did not migrate command handling; that remains deferred to Task 3.

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

Time Elapsed 00:00:01.96
```

No test project exists, and no test project or test framework was added per the task instructions. `git diff --check` also completed without findings.

## Files Changed

- `Matrix.Server/Services/ProtocolMessageSender.cs`
- `Matrix.Server/Program.cs`
- `.superpowers/sdd/2026-08-08-matrix-protocol-v1/task-2-report.md`

## Self-Review

- The implementation matches the exact signatures and serialization flow in the brief.
- Constructor null validation follows the existing nullable-code conventions.
- Cancellation tokens are passed through to both connection-manager operations.
- The service is registered as a singleton and no unrelated command, session, or WebSocket lifecycle code was changed.

## Concerns

None identified for Task 2. Command migration and protocol adoption by existing commands are intentionally deferred to Task 3.
