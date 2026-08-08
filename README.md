# Matrix

Matrix is a small WebSocket-based text world built with .NET 10. The server exposes a WebSocket endpoint at `/ws`, tracks connected sessions in memory, and lets players inspect rooms, see who is nearby, and move between rooms.

## Project Structure

- `Matrix.Core` contains domain types, strongly typed IDs, and the in-memory `WorldMap`.
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

When the server accepts the connection, it prompts for a username:

```text
Enter username:
```

Usernames are trimmed, must not be empty, and must be 24 characters or fewer.

## Commands

Commands must start with `/`. Non-command messages receive `Invalid command`.

| Command | Description |
| --- | --- |
| `/look` | Shows the current room name, description, exits, and users in the room. |
| `/who` | Lists users in the current room, or says you are alone. |
| `/go <direction>` | Moves to an adjacent room when an exit exists. |

`/go` accepts full directions and one-letter abbreviations:

- `north` or `n`
- `south` or `s`
- `east` or `e`
- `west` or `w`

If the direction is missing, invalid, or unavailable from the current room, the server sends a user-facing error.

Unknown slash commands receive:

```text
Unknown command: /command
```

## World

The current MVP world has two rooms:

- `Lobby`: the starting room. Exit: north to `Arcade`.
- `Arcade`: exit south to `Lobby`.

When a player moves, other players in the previous room are told that the player left, and players in the destination room are told that the player entered.

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
Welcome, Ada!
> /look
Lobby
Welcome to the Lobby
Exits: North
Users: Ada

> /who
You are alone in this room.
> /go north
You moved North to Arcade
Use /look to inspect room.

> /look
Arcade
Enjoy the arcade
Exits: South
Users: Ada

> /go west
You can not go West from here.
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
