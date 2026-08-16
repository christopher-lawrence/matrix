# Matrix

Matrix is a small WebSocket-based text world built with .NET 10. The server exposes a WebSocket endpoint at `/ws`, tracks connected sessions in memory, and lets players inspect areas, see who is nearby, and move between areas.

## Project Structure

- `Matrix.Core` contains domain types, strongly typed IDs, and the in-memory `World`.
- `Matrix.Server` is an ASP.NET Core WebSocket server exposing `GET /ws`.
- `Matrix.Client` is a console WebSocket client for connecting to the server.

## Prerequisites

- .NET 10 SDK
- A terminal

Optional:

- A WebSocket client such as `wscat`, `websocat`, or the included `Matrix.Client` console app

## Setup

Clone the repository and restore/build the solution:

```bash
git clone https://github.com/christopher-lawrence/matrix.git
cd matrix
dotnet build Matrix.slnx
```

## Run The Server

Start the WebSocket server:

```bash
dotnet run --project Matrix.Server
```

In the default development profile, the server listens on:

```text
http://localhost:5114
```

The WebSocket endpoint is:

```text
ws://localhost:5114/ws
```

Non-WebSocket HTTP requests to `/ws` return `400 Bad Request` with a message telling the caller to use a WebSocket client.

## Connect With The Console Client

In a second terminal, run:

```bash
dotnet run --project Matrix.Client
```

By default, the client connects to:

```text
ws://localhost:5114/ws
```

To connect to a different server URI, pass it as the first argument:

```bash
dotnet run --project Matrix.Client -- ws://localhost:5114/ws
```

Use `/quit` in the console client to exit locally.

## Connect With Another WebSocket Client

You can also connect with any WebSocket client. For example, with `wscat`:

```bash
wscat -c ws://localhost:5114/ws
```

When the server accepts the connection, it sends a JSON prompt:

```json
{"type":"prompt","data":{"message":"Enter username:"}}
```

Reply with a JSON `setUsername` message:

```json
{"type":"setUsername","args":{"username":"Chris"}}
```

Usernames are trimmed, must not be empty, and must be 24 characters or fewer.

## Commands

WebSocket clients send JSON messages with a `type` and optional `args` object. Responses are JSON envelopes with `type` and `data`.

| Message type | Description |
| --- | --- |
| `help` | Lists available commands with short examples. |
| `look` | Shows the current area name, description, exits, and users in the area. |
| `who` | Lists users in the current area. |
| `move` | Moves to an adjacent area when an exit exists. |
| `say` | Broadcasts a message to users in the current area. |

The WebSocket request examples are:

```text
> {"type":"look"}
> {"type":"move","args":{"direction":"north"}}
> {"type":"say","args":{"message":"hello"}}
> {"type":"who"}
> {"type":"help"}
```

`move` accepts full directions and one-letter abbreviations such as `north`/`n`, `south`/`s`, `east`/`e`, and `west`/`w`.

If the direction is missing, invalid, or unavailable from the current area, the server sends a user-facing error.

`say` trims the message before sending it. Empty or whitespace-only messages are rejected:

```json
{"type":"error","data":{"message":"You must provide a message to say."}}
```

Messages are broadcast only to users in the sender's current area. The sender also receives the broadcast.

Unknown message types receive:

```json
{"type":"error","data":{"message":"Unknown command."}}
```

## World

The current MVP world has two areas:

- `Lobby`: the starting area. Exit: north to `Arcade`.
- `Arcade`: exit south to `Lobby`.

When a player moves, other players in the previous area are told that the player left, and players in the destination area are told that the player entered.

## Example Session

Start the server:

```bash
dotnet run --project Matrix.Server
```

Start the client in another terminal:

```bash
dotnet run --project Matrix.Client
```

Example interaction:

```text
Connecting to ws://localhost:5114/ws...
Connected. Type /quit to exit.
Enter username:
> Ada
Ada entered.
> look
Lobby
Welcome to the Lobby
Exits: north
Users: Ada
> who
Users here: Ada
> say hello
Ada says: hello
> move north
Arcade
Enjoy the arcade
Exits: south
Users: Ada
> help
Available commands:
help - Lists available commands and examples.
  Example: help
look - Shows the current area, exits, and users nearby.
  Example: look
move - Moves to an adjacent area by direction.
  Example: move north
say - Broadcasts a message to users in your current area.
  Example: say hello
who - Lists users in your current area.
  Example: who
> /quit
```

## Development

Build the full solution:

```bash
dotnet build Matrix.slnx
```

Run the server:

```bash
dotnet run --project Matrix.Server
```

Run the client:

```bash
dotnet run --project Matrix.Client
```
