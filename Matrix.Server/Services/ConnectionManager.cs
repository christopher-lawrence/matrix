using System.Collections.Concurrent;
using System.Net.WebSockets;
using Matrix.Server.Extensions;

namespace Matrix.Server.Services;

public interface IConnectionManager
{
    bool TryAdd(Guid connectionId, WebSocket socket);
    bool TryGetSocket(Guid connectionId, out WebSocket? socket);
    Task RemoveAsync(Guid connectionId, CancellationToken ct = default);
    int Count { get; }
    Task<bool> SendTextAsync(Guid connectionId, string message, CancellationToken ct = default);
    Task BroadcastTextAsync(IEnumerable<Guid> connectionIds, string message, CancellationToken ct = default);
}

public sealed class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections;
    private readonly ILogger<ConnectionManager> _logger;

    public ConnectionManager(ILogger<ConnectionManager> logger)
    {
        _connections = new ConcurrentDictionary<Guid, WebSocket>();
        _logger = logger;
    }

    public int Count => _connections.Count;

    public bool TryAdd(Guid connectionId, WebSocket socket)
    {
        var added = _connections.TryAdd(connectionId, socket);
        if (!added)
        {
            _logger.LogError("Unable to add {ConnectionId}", connectionId);
            return false;
        }

        _logger.LogInformation(
            "Connection {ConnectionId} added. Active connections: {ActiveCount}",
            connectionId,
            Count);
        return true;
    }

    public bool TryGetSocket(Guid connectionId, out WebSocket? socket)
        => _connections.TryGetValue(connectionId, out socket);

    public async Task RemoveAsync(Guid connectionId, CancellationToken ct = default)
    {
        if (!_connections.TryRemove(connectionId, out WebSocket? socket))
        {
            _logger.LogWarning("Unable to remove {ConnectionId}", connectionId);
            return;
        }

        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing time", ct);
                }
                catch (WebSocketException ex)
                {
                    _logger.LogInformation(ex, "Socket {ConnectionId} disconnected before close completed", connectionId);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogInformation(ex, "Socket {ConnectionId} close was canceled", connectionId);
                }
            }
        }
        finally
        {
            socket.Dispose();
        }

        _logger.LogInformation(
            "Connection {ConnectionId} removed. Active connections: {ActiveCount}",
            connectionId,
            Count);
    }

    public async Task<bool> SendTextAsync(Guid connectionId, string message, CancellationToken ct = default)
    {
        if (!TryGetSocket(connectionId, out var socket) || socket is null)
        {
            _logger.LogWarning("Unable to find connection {ConnectionId} to send message", connectionId);
            return false;
        }

        if (socket.CloseStatus.HasValue)
        {
            _logger.LogWarning("Can not send message to closed socket {ConnectionId}", connectionId);
            return false;
        }

        await socket.SendTextAsync<ConnectionManager>(message, _logger, ct);
        return true;
    }

    public async Task BroadcastTextAsync(IEnumerable<Guid> connectionIds, string message, CancellationToken ct = default)
    {
        var targets = new HashSet<Guid>(connectionIds);
        foreach (var connectionId in targets)
        {
            await SendTextAsync(connectionId, message, ct);
        }
    }
}
