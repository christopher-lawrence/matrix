using Matrix.Core.Services;

namespace Matrix.Server.Services;

public sealed class WhoCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<WhoCommand> _logger;

    public WhoCommand(
        ISessionManager sessionManager,
        WorldMap worldMap,
        IConnectionManager connectionManager,
        ILogger<WhoCommand> logger)
    {
        _sessionManager = sessionManager;
        _worldMap = worldMap;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public string Name => "/who";

    public async Task ExecuteAsync(CommandContext context, string? parameters, CancellationToken ct)
    {
        if (!_sessionManager.TryGetByConnectionId(context.ConnectionId, out SessionState? session) || session is null)
        {
            _logger.LogError("Unable to find session for {ConnectionId}", context.ConnectionId);
            return;
        }

        if (!_worldMap.TryGetRoom(session.Value.CurrentRoomId, out var room) || room is null)
        {
            _logger.LogError("Unable to get room for id {CurrentRoomId}", session.Value.CurrentRoomId);
            return;
        }

        var sessions = _sessionManager.GetByRoom(room.Id);
        var users = sessions
            .Where(x => !string.IsNullOrEmpty(x.Username))
            .Select(x => x.Username)
            .OrderBy(x => x)
            .ToList();

        var message = users.Count > 1
            ? $"Users here: {string.Join(", ", users)}"
            : "You are alone in this room.";

        await _connectionManager.SendTextAsync(context.ConnectionId, message, ct);
    }
}
