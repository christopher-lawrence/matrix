using System.Net.WebSockets;
using Matrix.Core.Protocol;
using Matrix.Server.Extensions;

namespace Matrix.Server.Services;

public sealed class WebSocketConnectionService
{
    private readonly IConnectionManager _connectionManager;
    private readonly ISessionManager _sessionManager;
    private readonly IOnboardingService _onboardingService;
    private readonly ILogger<WebSocketConnectionService> _logger;
    private readonly ICommandHandler _commandHandler;

    public WebSocketConnectionService(
        ISessionManager sessionManager,
        IConnectionManager connectionManager,
        IOnboardingService onboardingService,
        ILogger<WebSocketConnectionService> logger,
        ICommandHandler commandHandler)
    {
        _sessionManager = sessionManager;
        _connectionManager = connectionManager;
        _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
    }

    public async Task AddConnectionAsync(Guid id, WebSocket webSocket)
    {
        if (!_connectionManager.TryAdd(id, webSocket))
        {
            return;
        }

        if (!await RunUsernameOnboardingAsync(id, CancellationToken.None))
        {
            _logger.LogWarning("Connection onboarding not completed for {ConnectionId}", id);
            await RemoveConnectionAsync(id);
            _sessionManager.RemoveByConnectionId(id, out _);
            return;
        }

        await HandleConnectionAsync(id, webSocket, CancellationToken.None);
    }

    public async Task RemoveConnectionAsync(Guid id)
    {
        await _connectionManager.RemoveAsync(id);
    }

    public int GetActiveCount() => _connectionManager.Count;

    public async Task<bool> SendTextAsync(Guid connectionId, string message, CancellationToken ct = default)
    {
        return await _connectionManager.SendTextAsync(connectionId, message, ct);
    }

    public async Task BroadcastTextAsync(IEnumerable<Guid> connectionIds, string message, CancellationToken ct = default)
    {
        await _connectionManager.BroadcastTextAsync(connectionIds, message, ct);
    }

    public async Task<bool> RunUsernameOnboardingAsync(Guid connectionId, CancellationToken ct = default)
    {
        if (!_connectionManager.TryGetSocket(connectionId, out var socket) || socket is null)
        {
            _logger.LogError("Unable to find socket {ConnectionId}", connectionId);
            return false;
        }

        var username = await _onboardingService.GetUsernameAsync(socket, ct);

        if (username is null)
        {
            _logger.LogWarning("Username onboarding did not produce a value for {ConnectionId}", connectionId);
            await socket.SendTextAsync(
                ProtocolJson.Serialize(new ServerMessage(
                    ProtocolMessageTypes.Error,
                    new ErrorData("Invalid username message. Send setUsername with a non-empty username of 24 characters or fewer."))),
                _logger,
                ct);
            return false;
        }

        if (!_sessionManager.TryUpdateUsername(connectionId, username))
        {
            await socket.SendTextAsync(
                ProtocolJson.Serialize(new ServerMessage(
                    ProtocolMessageTypes.Error,
                    new ErrorData("Unable to store username."))),
                _logger,
                ct);
            _logger.LogError("Unable to store username for {ConnectionId}", connectionId);
            return false;
        }

        await socket.SendTextAsync(
            ProtocolJson.Serialize(new ServerMessage(
                ProtocolMessageTypes.UserEntered,
                new UserPresenceData(username))),
            _logger,
            ct);

        if (!_sessionManager.TryGetByConnectionId(connectionId, out var session) || session is null)
        {
            await socket.SendTextAsync(
                ProtocolJson.Serialize(new ServerMessage(
                    ProtocolMessageTypes.Error,
                    new ErrorData("Unable to get session state."))),
                _logger,
                ct);
            _logger.LogError("Unable to get session state for {ConnectionId}", connectionId);
            return false;
        }

        _logger.LogInformation(
            "Connection {ConnectionId} completed onboarding for session {SessionId} in area {AreaId}",
            connectionId,
            session.Value.SessionId,
            session.Value.CurrentAreaId);

        var sessions = _sessionManager.GetByArea(session.Value.CurrentAreaId);

        var connectionIds = sessions.Where(x => x.ConnectionId != connectionId).Select(x => x.ConnectionId);

        await BroadcastTextAsync(
            connectionIds,
            ProtocolJson.Serialize(new ServerMessage(
                ProtocolMessageTypes.UserEntered,
                new UserPresenceData(username))),
            ct);

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
                    _logger.LogInformation(
                        "WebSocket receive completed without a message for {ConnectionId}. State: {WebSocketState}. Close status: {CloseStatus}",
                        id,
                        socket.State,
                        socket.CloseStatus);
                    break;
                }

                await _commandHandler.HandleMessage(message, id, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while handling connection {ConnectionId}", id);
        }
        finally
        {
            await RemoveConnectionAsync(id);

            if (_sessionManager.RemoveByConnectionId(id, out var removedSession) && removedSession is not null)
            {
                _logger.LogInformation(
                    "Removed session {SessionId} for disconnected connection {ConnectionId}",
                    removedSession.Value.SessionId,
                    id);
            }
            else
            {
                _logger.LogWarning("No session found while disconnecting connection {ConnectionId}", id);
            }
        }
    }
}
