using System.Collections.Concurrent;
using System.Net.WebSockets;
using Matrix.Server.Extensions;

namespace Matrix.Server.Services;

public sealed class WebSocketConnectionService
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _connections;
    private readonly ISessionManager _sessionManager;
    private readonly IOnboardingService _onboardingService;
    private readonly ILogger<WebSocketConnectionService> _logger;
    private readonly ICommandHandler _commandHandler;

    public WebSocketConnectionService(
        ISessionManager sessionManager,
        IOnboardingService onboardingService,
        ILogger<WebSocketConnectionService> logger,
        ICommandHandler commandHandler)
    {
        _sessionManager = sessionManager;
        _connections = new ConcurrentDictionary<Guid, WebSocket>();
        _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    }

    public async Task AddConnectionAsync(Guid id, WebSocket webSocket)
    {
        if (!_connections.TryAdd(id, webSocket))
        {
            _logger.LogError("Unable to add {ConnectionId}", id);
            return;
        }

        _logger.LogInformation("Added {ConnectionId}. {ActiveCount} current connections", id, GetActiveCount());

        if (!await RunUsernameOnboardingAsync(id, CancellationToken.None))
        {
            _logger.LogError("Unable to update username for {ConnectionId}", id);
            await RemoveConnectionAsync(id);
            _sessionManager.RemoveByConnectionId(id, out _);
            return;
        }

        await HandleConnectionAsync(id, webSocket, CancellationToken.None);
    }

    public async Task RemoveConnectionAsync(Guid id)
    {
        if (_connections.TryRemove(id, out WebSocket? ws))
        {
            if (!ws.CloseStatus.HasValue)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing time", CancellationToken.None);
            }

            ws.Dispose();
            _logger.LogInformation("Removed {ConnectionId}. {ActiveCount} current connections", id, GetActiveCount());
        }
        else
        {
            _logger.LogWarning("Unable to remove {ConnectionId}", id);
        }
    }

    public int GetActiveCount() => _connections.Count();

    public async Task<bool> SendTextAsync(Guid connectionId, string message, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(connectionId, out var socket))
        {
            _logger.LogWarning("Unable to find connection {ConnectionId} to send message", connectionId);
            return false;
        }

        if (socket.CloseStatus.HasValue)
        {
            _logger.LogWarning("Can not send message to closed socket {ConnectionId}", connectionId);
            return false;
        }

        await socket.SendTextAsync<WebSocketConnectionService>(message, _logger, ct);

        return true;
    }

    public async Task BroadcastTextAsync(IEnumerable<Guid> connectionIds, string message, CancellationToken ct = default)
    {
        var connections = new HashSet<Guid>(connectionIds);

        foreach (var connection in connections)
        {
            await SendTextAsync(connection, message, ct);
        }
    }

    public async Task<bool> RunUsernameOnboardingAsync(Guid connectionId, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(connectionId, out var socket))
        {
            _logger.LogError("Unable to find socket {ConnectionId}", connectionId);
            return false;
        }

        var username = await _onboardingService.GetUsernameAsync(socket, ct);

        if (username is null)
        {
            _logger.LogError("Invalid username entered {Username}", username);
            await socket.SendTextAsync("Invalid username. Must not be empty and less than 24 characters", _logger, ct);
            return false;
        }

        if (!_sessionManager.TryUpdateUsername(connectionId, username))
        {
            await socket.SendTextAsync("Unable to store username", _logger, ct);
            _logger.LogError("Unable to store {Username} for {ConnectionId}", username, connectionId);
            return false;
        }

        await socket.SendTextAsync($"Welcome, {username}!", _logger, ct);

        if (!_sessionManager.TryGetByConnectionId(connectionId, out var session) || session is null)
        {
            await socket.SendTextAsync("Unable to get session state", _logger, ct);
            _logger.LogError("Unable to get session state for {ConnectionId}", connectionId);
            return false;
        }

        var sessions = _sessionManager.GetByRoom(session.Value.CurrentRoomId);

        var connectionIds = sessions.Where(x => x.ConnectionId != connectionId).Select(x => x.ConnectionId);

        await BroadcastTextAsync(connectionIds, $"{username} joined the lobby", ct);

        return true;
    }

    private async Task HandleConnectionAsync(Guid id, WebSocket socket, CancellationToken ct)
    {
        try
        {
            while (!socket.CloseStatus.HasValue)
            {
                var message = await socket.ReceiveMessageAsync(_logger, ct);

                if (message is null)
                {
                    _logger.LogInformation("Closing socket room {ConnectionId}", id);
                    break;
                }

                await _commandHandler.HandleMessage(message, id, socket, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while handling connection");
        }
        finally
        {
            await RemoveConnectionAsync(id);
            _sessionManager.RemoveByConnectionId(id, out var _);
        }
    }
}
