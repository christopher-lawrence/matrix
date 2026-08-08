using System.Net.WebSockets;
using System.Text;

var serverUri = GetServerUri(args);
using var shutdown = new CancellationTokenSource();
using var socket = new ClientWebSocket();
var shutdownRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

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

    var receiveTask = ReceiveMessagesAsync(socket, shutdown);
    var sendTask = SendConsoleInputAsync(socket, shutdown);

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

static async Task ReceiveMessagesAsync(ClientWebSocket socket, CancellationTokenSource shutdown)
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
            Console.WriteLine(text);
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

static async Task SendConsoleInputAsync(ClientWebSocket socket, CancellationTokenSource shutdown)
{
    try
    {
        while (!shutdown.IsCancellationRequested && socket.State == WebSocketState.Open)
        {
            var line = await Console.In.ReadLineAsync(shutdown.Token);
            if (line is null)
            {
                shutdown.Cancel();
                return;
            }

            if (line.Equals("/quit", StringComparison.OrdinalIgnoreCase))
            {
                shutdown.Cancel();
                return;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var bytes = Encoding.UTF8.GetBytes(line);
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
