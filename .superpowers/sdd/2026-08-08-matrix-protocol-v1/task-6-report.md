# Task 6: Documentation And Manual Verification

## What Was Implemented

- Updated `README.md` to document Matrix Protocol v1 JSON WebSocket messages.
- Replaced slash-command WebSocket examples with the required `look`, `move`, `say`, `who`, and `help` JSON messages.
- Documented that server responses are JSON envelopes with `type` and `data`.
- Updated the README example session to use the console client's command syntax while retaining local `/quit` usage.

## Build Output

Command:

```bash
dotnet build Matrix.slnx
```

Result:

```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  Matrix.Core -> /Users/chris/src/matrix/Matrix.Core/bin/Debug/net10.0/Matrix.Core.dll
  Matrix.Client -> /Users/chris/src/matrix/Matrix.Client/bin/Debug/net10.0/Matrix.Client.dll
  Matrix.Server -> /Users/chris/src/matrix/Matrix.Server/bin/Debug/net10.0/Matrix.Server.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.70
```

## Files Changed

- `README.md`
- `.superpowers/sdd/2026-08-08-matrix-protocol-v1/task-6-report.md`

## Self-Review

- The required JSON request examples are present verbatim.
- The README no longer presents slash commands as the WebSocket protocol.
- Response envelopes with `type` and `data` are documented.
- No source code or test projects were modified.
- `git diff --check` completed without whitespace errors.

## Concerns And Handoff

The live WebSocket manual verification remains for the controller. This task did not start a long-running server or attempt interactive WebSocket checks. The controller should verify invalid JSON, unknown message types, missing or invalid arguments, existing protocol behavior, slash-command rejection, and the console client behavior described in the task brief.

## Controller Manual Verification

The controller started the server with:

```bash
dotnet run --project Matrix.Server
```

Server listened on:

```text
http://localhost:5114
```

The controller connected with `wscat --no-color -c ws://localhost:5114/ws` and observed the JSON prompt:

```json
{"type":"prompt","data":{"message":"Enter username:"}}
```

After sending:

```json
{"type":"setUsername","args":{"username":"ManualOne"}}
```

the server returned:

```json
{"type":"userEntered","data":{"username":"ManualOne"}}
```

The following error checks returned JSON `error` envelopes:

```text
not json
```

returned:

```json
{"type":"error","data":{"message":"Invalid JSON."}}
```

```json
{"type":"dance"}
```

returned:

```json
{"type":"error","data":{"message":"Unknown command."}}
```

```json
{"type":"move"}
{"type":"move","args":{"direction":"up"}}
{"type":"say"}
{"type":"say","args":{"message":"   "}}
```

returned clear JSON `error` envelopes for missing direction, invalid direction, and missing message.

The following existing behavior checks returned structured JSON:

```json
{"type":"look"}
{"type":"who"}
{"type":"move","args":{"direction":"north"}}
{"type":"say","args":{"message":"hello"}}
{"type":"help"}
```

Observed response types:

- `roomState` with `id`, `name`, `description`, `users`, and `exits`
- `who` with `users`
- destination `roomState` after `move`
- `chatMessage` after `say`
- `help` with command metadata

Sending the old slash command:

```text
/look
```

returned:

```json
{"type":"error","data":{"message":"Invalid JSON."}}
```

Two-client presence verification:

- ManualOne and ManualTwo joined the lobby.
- ManualOne received `{"type":"userEntered","data":{"username":"ManualTwo"}}`.
- ManualTwo moved north; ManualOne received `{"type":"userLeft","data":{"username":"ManualTwo"}}`.
- ManualOne moved north; ManualTwo received `{"type":"userEntered","data":{"username":"ManualOne"}}`.

Console client smoke verification used:

```bash
dotnet run --project Matrix.Client
```

Observed behavior:

- The client rendered the server JSON prompt as `Enter username:`.
- Entering `ConsoleTwo` sent username onboarding and rendered `ConsoleTwo entered.`
- `look` rendered room name, description, exits, and users.
- `who` rendered `Users here: ConsoleTwo`.
- `move north` rendered the Arcade room state.
- `say hello from console` rendered `ConsoleTwo says: hello from console`.
- `help` rendered the five protocol commands and JSON examples.
- `/quit` closed the client locally.

## Review Fix

Updated the `wscat` onboarding documentation in `README.md` to show the JSON `prompt` frame sent by the server and the JSON `setUsername` frame required from the client:

```json
{"type":"prompt","data":{"message":"Enter username:"}}
{"type":"setUsername","args":{"username":"Chris"}}
```

Validation command:

```bash
dotnet build Matrix.slnx
```

Validation output:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Files changed for this fix:

- `README.md`
- `.superpowers/sdd/2026-08-08-matrix-protocol-v1/task-6-report.md`
