using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Matrix.Core.Protocol;

var serverUri = GetServerUri(args);
using var shutdown = new CancellationTokenSource();
using var socket = new ClientWebSocket();
var shutdownRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var usernamePromptReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
var usernameSent = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var usernameEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Console.WriteLine();
    shutdown.Cancel();
    shutdownRequested.TrySetResult();
};

try
{
    Console.WriteLine($"Connecting to {serverUri}...");
    await socket.ConnectAsync(serverUri, shutdown.Token);
    Console.WriteLine("Connected. Type /quit to exit.");

    var receiveTask = ReceiveMessagesAsync(
        socket,
        shutdown,
        () => { usernamePromptReceived.TrySetResult(); },
        username =>
        {
            if (usernameSent.Task.IsCompletedSuccessfully
                && username.Equals(usernameSent.Task.Result, StringComparison.Ordinal))
            {
                usernameEntered.TrySetResult();
            }
        });
    var sendTask = SendConsoleInputAsync(
        socket,
        shutdown,
        usernamePromptReceived.Task,
        usernameSent,
        usernameEntered.Task);

    await Task.WhenAny(receiveTask, sendTask, shutdownRequested.Task);
    shutdown.Cancel();
    await Task.WhenAll(receiveTask, sendTask);
}
catch (OperationCanceledException)
{
}
catch (WebSocketException ex)
{
    Console.WriteLine($"WebSocket error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Client error: {ex.Message}");
}
finally
{
    shutdown.Cancel();
    await CloseSocketAsync(socket);
}

static Uri GetServerUri(string[] args)
{
    if (args.Length == 0)
    {
        return new Uri("ws://localhost:5114/ws");
    }

    if (!Uri.TryCreate(args[0], UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeWs && uri.Scheme != Uri.UriSchemeWss))
    {
        Console.WriteLine("Usage: dotnet run --project Matrix.Client [ws://host:port/ws]");
        Environment.ExitCode = 1;
        Environment.Exit(Environment.ExitCode);
    }

    return uri;
}

static async Task ReceiveMessagesAsync(
    ClientWebSocket socket,
    CancellationTokenSource shutdown,
    Action usernamePromptReceived,
    Action<string> usernameEntered)
{
    var buffer = new byte[4096];

    try
    {
        while (!shutdown.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(buffer, shutdown.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Server disconnected.");
                    shutdown.Cancel();
                    return;
                }

                message.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                Console.WriteLine($"Received unsupported {result.MessageType} message.");
                continue;
            }

            var text = Encoding.UTF8.GetString(message.ToArray());
            if (!TryRenderServerMessage(text, usernamePromptReceived, usernameEntered, out var rendered) || rendered is null)
            {
                Console.WriteLine(text);
            }
            else
            {
                Console.WriteLine(rendered);
            }

            WritePrompt();
        }
    }
    catch (OperationCanceledException)
    {
    }
    catch (WebSocketException ex)
    {
        Console.WriteLine($"Disconnected: {ex.Message}");
        shutdown.Cancel();
    }
}

static async Task SendConsoleInputAsync(
    ClientWebSocket socket,
    CancellationTokenSource shutdown,
    Task usernamePromptReceived,
    TaskCompletionSource<string> usernameSubmission,
    Task usernameEntered)
{
    try
    {
        await usernamePromptReceived.WaitAsync(shutdown.Token);
        var isUsernameSent = false;

        while (!shutdown.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var line = await Console.In.ReadLineAsync(shutdown.Token);
            if (line is null)
            {
                shutdown.Cancel();
                return;
            }

            if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase)
                || line.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                if (usernameSubmission.Task.IsCompletedSuccessfully)
                {
                    await usernameEntered.WaitAsync(shutdown.Token);
                }

                shutdown.Cancel();
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                WritePrompt();
                continue;
            }

            ClientMessage? clientMessage;
            if (!isUsernameSent)
            {
                clientMessage = new ClientMessage(
                    ProtocolMessageTypes.SetUsername,
                    JsonSerializer.SerializeToElement(new UsernameArgs(line.Trim()), ProtocolJson.SerializerOptions));
                usernameSubmission.TrySetResult(line.Trim());
                isUsernameSent = true;
            }
            else if (!TryCreateClientMessage(line, out clientMessage) || clientMessage is null)
            {
                Console.WriteLine("Unknown command. Use look, who, move <direction>, say <message>, help, or quit.");
                WritePrompt();
                continue;
            }

            var json = JsonSerializer.Serialize(clientMessage, ProtocolJson.SerializerOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, shutdown.Token);
        }
    }
    catch (OperationCanceledException)
    {
    }
    catch (WebSocketException ex)
    {
        Console.WriteLine($"Send failed: {ex.Message}");
        shutdown.Cancel();
    }
}

static bool TryCreateClientMessage(string line, out ClientMessage? message)
{
    message = null;
    var trimmed = line.Trim();

    if (trimmed.Equals("look", StringComparison.OrdinalIgnoreCase))
    {
        message = new ClientMessage(ProtocolMessageTypes.Look);
        return true;
    }

    if (trimmed.Equals("who", StringComparison.OrdinalIgnoreCase))
    {
        message = new ClientMessage(ProtocolMessageTypes.Who);
        return true;
    }

    if (trimmed.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        message = new ClientMessage(ProtocolMessageTypes.Help);
        return true;
    }

    if (trimmed.StartsWith("move ", StringComparison.OrdinalIgnoreCase))
    {
        var direction = trimmed["move ".Length..].Trim();
        message = new ClientMessage(
            ProtocolMessageTypes.Move,
            JsonSerializer.SerializeToElement(new MoveArgs(direction), ProtocolJson.SerializerOptions));
        return true;
    }

    if (trimmed.StartsWith("say ", StringComparison.OrdinalIgnoreCase))
    {
        var text = trimmed["say ".Length..].Trim();
        message = new ClientMessage(
            ProtocolMessageTypes.Say,
            JsonSerializer.SerializeToElement(new SayArgs(text), ProtocolJson.SerializerOptions));
        return true;
    }

    return false;
}

static bool TryRenderServerMessage(
    string json,
    Action usernamePromptReceived,
    Action<string> usernameEntered,
    out string? rendered)
{
    rendered = null;

    try
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("type", out var typeElement))
        {
            return false;
        }

        var type = typeElement.GetString();
        var data = document.RootElement.TryGetProperty("data", out var dataElement)
            ? dataElement
            : default;

        if (type == ProtocolMessageTypes.Prompt)
        {
            usernamePromptReceived();
        }
        else if (type == ProtocolMessageTypes.UserEntered
            && data.TryGetProperty("username", out var enteredUsername)
            && enteredUsername.GetString() is { } username)
        {
            usernameEntered(username);
        }

        rendered = type switch
        {
            ProtocolMessageTypes.Prompt => data.TryGetProperty("message", out var promptMessage)
                ? promptMessage.GetString()
                : "Enter username:",
            ProtocolMessageTypes.Error => data.TryGetProperty("message", out var message)
                ? $"Error: {message.GetString()}"
                : "Error",
            ProtocolMessageTypes.ChatMessage => data.TryGetProperty("sender", out var sender)
                && data.TryGetProperty("message", out var chatMessage)
                    ? $"{sender.GetString()} says: {chatMessage.GetString()}"
                    : "Chat message",
            ProtocolMessageTypes.UserEntered => data.TryGetProperty("username", out var entered)
                ? $"{entered.GetString()} entered."
                : "User entered.",
            ProtocolMessageTypes.UserLeft => data.TryGetProperty("username", out var left)
                ? $"{left.GetString()} left."
                : "User left.",
            ProtocolMessageTypes.RoomState => RenderRoomState(data),
            ProtocolMessageTypes.Who => RenderWho(data),
            ProtocolMessageTypes.Help => RenderHelp(data),
            _ => json
        };

        return true;
    }
    catch (JsonException)
    {
        return false;
    }
}

static string RenderRoomState(JsonElement data)
{
    var name = data.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : "Area";
    var description = data.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : "";
    var exits = ReadStringArray(data, "exits");
    var users = ReadStringArray(data, "users");

    return $"""
{name}
{description}
Exits: {(exits.Count == 0 ? "none" : string.Join(", ", exits))}
Users: {(users.Count == 0 ? "none" : string.Join(", ", users))}
""";
}

static string RenderWho(JsonElement data)
{
    var users = ReadStringArray(data, "users");
    return users.Count == 0 ? "You are alone in this area." : $"Users here: {string.Join(", ", users)}";
}

static string RenderHelp(JsonElement data)
{
    if (!data.TryGetProperty("commands", out var commands) || commands.ValueKind != JsonValueKind.Array)
    {
        return "No commands available.";
    }

    var lines = new List<string> { "Available commands:" };
    foreach (var command in commands.EnumerateArray())
    {
        var type = command.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "";
        var description = command.TryGetProperty("description", out var descriptionElement) ? descriptionElement.GetString() : "";
        var example = command.TryGetProperty("example", out var exampleElement) ? exampleElement.GetString() : "";
        lines.Add($"{type} - {description}");
        lines.Add($"  Example: {example}");
    }

    return string.Join(Environment.NewLine, lines);
}

static IReadOnlyList<string> ReadStringArray(JsonElement data, string propertyName)
{
    if (!data.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
    {
        return Array.Empty<string>();
    }

    return array.EnumerateArray()
        .Select(x => x.GetString())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!)
        .ToList();
}

static void WritePrompt() => Console.Write("> ");

static async Task CloseSocketAsync(ClientWebSocket socket)
{
    if (socket.State is not WebSocketState.Open and not WebSocketState.CloseReceived)
    {
        return;
    }

    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

    try
    {
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", timeout.Token);
    }
    catch (WebSocketException)
    {
    }
    catch (OperationCanceledException)
    {
    }
}
