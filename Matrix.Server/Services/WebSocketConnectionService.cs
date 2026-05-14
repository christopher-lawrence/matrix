using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Matrix.Server.Services;

public sealed class WebSocketConnectionService
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections;
    private readonly ILogger<WebSocketConnectionService> _logger;

    public WebSocketConnectionService(ILogger<WebSocketConnectionService> logger)
    {
        _connections = new ConcurrentDictionary<Guid, WebSocket>();
        _logger = logger;
    }

    public async Task AddConnectionAsync(Guid id, WebSocket webSocket)
    {
        if (!_connections.TryAdd(id, webSocket))
        {
            _logger.LogError($"Unable to add {id}");
            return;
        }

        _logger.LogInformation($"Added {id}. {GetActiveCount()} current connections");

        await HandleConnectionAsync(id, webSocket, CancellationToken.None);
    }

    public async Task RemoveConnection(Guid id)
    {
        if (_connections.TryRemove(id, out WebSocket? ws))
        {
            if (!ws.CloseStatus.HasValue)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing time", CancellationToken.None);
            }

            ws.Dispose();
            _logger.LogInformation($"Removed {id}. {GetActiveCount()} current connections");
        }
        else
        {
            _logger.LogWarning($"Unable to remove {id}");
        }
    }

    public int GetActiveCount() => _connections.Count();

    private async Task HandleConnectionAsync(Guid id, WebSocket socket, CancellationToken ct)
    {
        try
        {
            while (!socket.CloseStatus.HasValue)
            {
                var buffer = new byte[1024 * 4];
                var receiveResult = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), ct);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    _logger.LogInformation($"Received Close message from {id}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while handling connection");
        }
        finally
        {
            await RemoveConnection(id);
        }
    }
}
