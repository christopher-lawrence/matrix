using System.Collections.Concurrent;
using Matrix.Core.Ids;

namespace Matrix.Server.Services;

public interface ISessionManager
{
    bool TryAdd(SessionState session);
    bool TryGetByConnectionId(Guid connectionId, out SessionState? session);
    bool TryGetBySessionId(UserSessionId sessionId, out SessionState? session);
    bool RemoveByConnectionId(Guid connectionId, out SessionState? removed);
    bool RemoveBySessionId(UserSessionId sessionId, out SessionState? removed);
    IReadOnlyList<SessionState> GetByRoom(RoomId roomId);
    int Count { get; }
    bool TryUpdateUsername(Guid connectionId, string username);
    bool TryUpdateRoom(Guid connectionId, RoomId roomId);
}

public sealed class SessionManager : ISessionManager
{
    private readonly ConcurrentDictionary<Guid, SessionState> _byConnectionId;
    private readonly ConcurrentDictionary<UserSessionId, Guid> _connectionBySessionId;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(ILogger<SessionManager> logger)
    {
        _byConnectionId = new ConcurrentDictionary<Guid, SessionState>();
        _connectionBySessionId = new ConcurrentDictionary<UserSessionId, Guid>();
        _logger = logger;
    }

    public int Count => _connectionBySessionId.Count;

    public IReadOnlyList<SessionState> GetByRoom(RoomId roomId)
    {
        return _byConnectionId.Values
            .Where(x => x.CurrentRoomId == roomId)
            .ToList();
    }

    public bool RemoveByConnectionId(Guid connectionId, out SessionState? removed)
    {
        removed = null;
        if (!_byConnectionId.TryRemove(connectionId, out var sessionState))
        {
            return false;
        }

        removed = sessionState;
        if (!_connectionBySessionId.TryRemove(sessionState.SessionId, out _))
        {
            _logger.LogWarning("Unable to remove connection {SessionId}", sessionState.SessionId);
        }

        return true;
    }

    public bool RemoveBySessionId(UserSessionId sessionId, out SessionState? removed)
    {
        removed = null;
        if (!_connectionBySessionId.TryRemove(sessionId, out var connectionId))
        {
            _logger.LogWarning("Unable to remove by sessionId {SessionId}", sessionId);
            return false;
        }

        if (!_byConnectionId.TryRemove(connectionId, out var sessionState))
        {
            _logger.LogWarning("Unable to remove by connectionId {ConnectionId}", connectionId);
            return false;
        }

        removed = sessionState;
        return true;

    }

    public bool TryAdd(SessionState session)
    {
        if (!_byConnectionId.TryAdd(session.ConnectionId, session))
        {
            _logger.LogWarning(
                "Unable to add session by connectionId {ConnectionId}", session.ConnectionId);
            return false;
        }

        if (!_connectionBySessionId.TryAdd(session.SessionId, session.ConnectionId))
        {
            _logger.LogWarning(
                "Unable to add connection by sessionId {SessionId}", session.SessionId);
            _byConnectionId.TryRemove(session.ConnectionId, out _);
            return false;
        }

        return true;
    }

    public bool TryGetByConnectionId(Guid connectionId, out SessionState? session)
    {
        session = null;
        if (!_byConnectionId.TryGetValue(connectionId, out var sessionState))
        {
            return false;
        }

        session = sessionState;
        return true;
    }

    public bool TryGetBySessionId(UserSessionId sessionId, out SessionState? session)
    {
        session = null;
        if (!_connectionBySessionId.TryGetValue(sessionId, out var connectionId))
        {
            return false;
        }

        return TryGetByConnectionId(connectionId, out session);
    }

    public bool TryUpdateRoom(Guid connectionId, RoomId roomId)
    {
        if (!TryGetByConnectionId(connectionId, out SessionState? session) || session is null)
        {
            return false;
        }

        var updatedSession = session.Value with { CurrentRoomId = roomId };

        _byConnectionId[connectionId] = updatedSession;

        return true;
    }

    public bool TryUpdateUsername(Guid connectionId, string username)
    {
        if (!TryGetByConnectionId(connectionId, out SessionState? session) || session is null)
        {
            return false;
        }

        var updatedSession = session.Value with { Username = username };

        _byConnectionId[connectionId] = updatedSession;

        return true;
    }
}
