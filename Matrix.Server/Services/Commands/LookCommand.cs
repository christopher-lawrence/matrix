using System.Text.Json;
using Matrix.Core.Domain;
using Matrix.Core.Protocol;

namespace Matrix.Server.Services.Commands;

public sealed class LookCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly World _world;
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly ILogger<LookCommand> _logger;

    public LookCommand(
        ISessionManager sessionManager,
        World world,
        IProtocolMessageSender protocolMessageSender,
        ILogger<LookCommand> logger)
    {
        _sessionManager = sessionManager;
        _world = world;
        _protocolMessageSender = protocolMessageSender;
        _logger = logger;
    }

    public string Type => ProtocolMessageTypes.Look;

    public string Description => "Shows the current area, exits, and users nearby.";

    public string Example => "look";

    public async Task ExecuteAsync(CommandContext context, JsonElement? args, CancellationToken ct)
    {
        if (!_sessionManager.TryGetByConnectionId(context.ConnectionId, out SessionState? session) || session is null)
        {
            _logger.LogError("Unable to find session for {ConnectionId}", context.ConnectionId);
            return;
        }

        if (!_world.TryGetArea(session.Value.CurrentAreaId, out var area) || area is null)
        {
            _logger.LogError("Unable to get area for id {CurrentAreaId}", session.Value.CurrentAreaId);
            return;
        }

        var sessions = _sessionManager.GetByArea(area.Id);
        var users = sessions
            .Where(x => !string.IsNullOrWhiteSpace(x.Username))
            .Select(x => x.Username)
            .OrderBy(x => x)
            .ToList();
        var exits = area.Connections
            .Select(x => x.Direction.ToString().ToLowerInvariant())
            .OrderBy(x => x)
            .ToList();

        await _protocolMessageSender.SendAsync(
            context.ConnectionId,
            ProtocolMessageTypes.RoomState,
            new RoomStateData(
                area.Id.Value.ToString(),
                area.Name,
                area.Description ?? $"Welcome to {area.Name}",
                users,
                exits),
            ct);
    }
}
