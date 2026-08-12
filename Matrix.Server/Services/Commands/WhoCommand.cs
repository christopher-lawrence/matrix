using System.Text.Json;
using Matrix.Core.Domain;
using Matrix.Core.Protocol;

namespace Matrix.Server.Services.Commands;

public sealed class WhoCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly World _world;
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly ILogger<WhoCommand> _logger;

    public WhoCommand(
        ISessionManager sessionManager,
        World world,
        IProtocolMessageSender protocolMessageSender,
        ILogger<WhoCommand> logger)
    {
        _sessionManager = sessionManager;
        _world = world;
        _protocolMessageSender = protocolMessageSender;
        _logger = logger;
    }

    public string Type => ProtocolMessageTypes.Who;

    public string Description => "Lists users in your current area.";

    public string Example => "who";

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

        var users = _sessionManager.GetByArea(area.Id)
            .Where(x => !string.IsNullOrWhiteSpace(x.Username))
            .Select(x => x.Username)
            .OrderBy(x => x)
            .ToList();

        await _protocolMessageSender.SendAsync(
            context.ConnectionId,
            ProtocolMessageTypes.Who,
            new WhoData(users),
            ct);
    }
}
