using System.Text;
using Matrix.Core.Services;

namespace Matrix.Server.Services.Commands;

public sealed class LookCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<LookCommand> _logger;

    public LookCommand(
        ISessionManager sessionManager,
        WorldMap worldMap,
        IConnectionManager connectionManager,
        ILogger<LookCommand> logger)
    {
        _sessionManager = sessionManager;
        _worldMap = worldMap;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public string Name => "/look";

    public string Description => "Shows the current room, exits, and users nearby.";

    public string Example => "/look";

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
        var users = sessions.Select(x => x.Username);
        var usersMessage = users.Any() ? string.Join(", ", users) : "none";
        var exits = room.Exits.Any()
            ? string.Join(", ", room.Exits.Select(x => x.Key.ToString()).OrderBy(x => x))
            : "none";

        var sb = new StringBuilder();
        sb.AppendLine(room.Name);
        sb.AppendLine(room.Description ?? $"Welcome to {room.Name}");
        sb.AppendLine($"Exits: {exits}");
        sb.AppendLine($"Users: {usersMessage}");

        await _connectionManager.SendTextAsync(context.ConnectionId, sb.ToString(), ct);
    }
}
