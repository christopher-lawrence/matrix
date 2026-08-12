
using System.Text.Json;
using System.Text.RegularExpressions;
using Matrix.Core.Domain;
using Matrix.Core.Ids;
using Matrix.Core.Protocol;

namespace Matrix.Server.Services.Commands;

public sealed class GoCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly World _world;
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly ILogger<GoCommand> _logger;

    public GoCommand(
        ISessionManager sessionManager,
        World world,
        IProtocolMessageSender protocolMessageSender,
        ILogger<GoCommand> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _world = world ?? throw new ArgumentNullException(nameof(world));
        _protocolMessageSender = protocolMessageSender ?? throw new ArgumentNullException(nameof(protocolMessageSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => ProtocolMessageTypes.Move;

    public string Description => "Moves to an adjacent area by direction.";

    public string Example => "move north";

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

        var moveArgs = ProtocolJson.DeserializeArgs<MoveArgs>(args);
        var directionText = moveArgs?.Direction?.Trim();
        if (string.IsNullOrEmpty(directionText))
        {
            await _protocolMessageSender.SendAsync(
                context.ConnectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("You must specify a direction: north, south, east, west."),
                ct);
            return;
        }

        if (!TryParseDirection(directionText, out Direction? direction) || direction is null)
        {
            _logger.LogWarning("Unable to parse direction for {CommandType} from {ConnectionId}", Type, context.ConnectionId);
            await _protocolMessageSender.SendAsync(
                context.ConnectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("Invalid direction: north, south, east, west."),
                ct);
            return;
        }

        await TryMoveAsync(context, session.Value, direction.Value, ct);
    }

    private async Task TryMoveAsync(CommandContext context, SessionState session, Direction direction, CancellationToken ct)
    {
        var previousAreaId = session.CurrentAreaId;

        if (!_world.TryMove(session.CurrentAreaId, direction, out AreaId areaId) || areaId == default)
        {
            _logger.LogInformation("Can not move {Direction} in {AreaId}", direction, session.CurrentAreaId.Value);
            await _protocolMessageSender.SendAsync(
                context.ConnectionId,
                ProtocolMessageTypes.Error,
                new ErrorData($"You can not go {direction} from here."),
                ct);
            return;
        }

        if (!_sessionManager.TryUpdateArea(session.ConnectionId, areaId))
        {
            _logger.LogError("Unable to update area for {ConnectionId} to {AreaId}", session.ConnectionId, areaId);
            return;
        }

        // Broadcast leave
        var previousAreaConnectionIds = _sessionManager
            .GetByArea(previousAreaId)
            .Where(x => x.ConnectionId != session.ConnectionId)
            .Select(x => x.ConnectionId)
            .ToList();
        await _protocolMessageSender.BroadcastAsync(
            previousAreaConnectionIds,
            ProtocolMessageTypes.UserLeft,
            new UserPresenceData(session.Username),
            ct);

        // Broadcast enter
        var currentAreaConnectionIds = _sessionManager
            .GetByArea(areaId)
            .Where(x => x.ConnectionId != session.ConnectionId)
            .Select(x => x.ConnectionId)
            .ToList();
        await _protocolMessageSender.BroadcastAsync(
            currentAreaConnectionIds,
            ProtocolMessageTypes.UserEntered,
            new UserPresenceData(session.Username),
            ct);

        // Send message to user
        if (!_world.TryGetArea(areaId, out var area) || area is null)
        {
            _logger.LogError("Unable to get area for {AreaId}", areaId);
            return;
        }

        var sessions = _sessionManager.GetByArea(area.Id);
        var users = sessions
            .Where(x => !string.IsNullOrWhiteSpace(x.Username))
            .Select(x => x.Username)
            .OrderBy(x => x)
            .ToList();
        var exits = area.Exits.Keys
            .Select(x => x.ToString().ToLowerInvariant())
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

    private static bool TryParseDirection(string input, out Direction? direction)
    {
        direction = input switch
        {
            string n when Regex.IsMatch(n, @"^[nN](orth)?$") => Direction.North,
            string s when Regex.IsMatch(s, @"^[sS](outh)?$") => Direction.South,
            string e when Regex.IsMatch(e, @"^[eE](ast)?$") => Direction.East,
            string w when Regex.IsMatch(w, @"^[wW](est)?$") => Direction.West,
            _ => null
        };

        return direction is not null;
    }
}
