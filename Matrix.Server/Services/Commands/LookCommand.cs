using System.Text.Json;
using Matrix.Core.Protocol;
using Matrix.Core.Services;

namespace Matrix.Server.Services.Commands;

public sealed class LookCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly ILogger<LookCommand> _logger;

    public LookCommand(
        ISessionManager sessionManager,
        WorldMap worldMap,
        IProtocolMessageSender protocolMessageSender,
        ILogger<LookCommand> logger)
    {
        _sessionManager = sessionManager;
        _worldMap = worldMap;
        _protocolMessageSender = protocolMessageSender;
        _logger = logger;
    }

    public string Type => ProtocolMessageTypes.Look;

    public string Description => "Shows the current room, exits, and users nearby.";

    public string Example => """{"type":"look"}""";

    public async Task ExecuteAsync(CommandContext context, JsonElement? args, CancellationToken ct)
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
            .Where(x => !string.IsNullOrWhiteSpace(x.Username))
            .Select(x => x.Username)
            .OrderBy(x => x)
            .ToList();
        var exits = room.Exits.Keys
            .Select(x => x.ToString().ToLowerInvariant())
            .OrderBy(x => x)
            .ToList();

        await _protocolMessageSender.SendAsync(
            context.ConnectionId,
            ProtocolMessageTypes.RoomState,
            new RoomStateData(
                room.Id.Value.ToString(),
                room.Name,
                room.Description ?? $"Welcome to {room.Name}",
                users,
                exits),
            ct);
    }
}
