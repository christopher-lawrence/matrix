
using System.Text.Json;
using System.Text.RegularExpressions;
using Matrix.Core.Domain;
using Matrix.Core.Ids;
using Matrix.Core.Protocol;
using Matrix.Core.Services;

namespace Matrix.Server.Services.Commands;

public sealed class GoCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly ILogger<GoCommand> _logger;

    public GoCommand(
        ISessionManager sessionManager,
        WorldMap worldMap,
        IProtocolMessageSender protocolMessageSender,
        ILogger<GoCommand> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _worldMap = worldMap ?? throw new ArgumentNullException(nameof(worldMap));
        _protocolMessageSender = protocolMessageSender ?? throw new ArgumentNullException(nameof(protocolMessageSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => ProtocolMessageTypes.Move;

    public string Description => "Moves to an adjacent room by direction.";

    public string Example => "move north";

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
        var previousRoomId = session.CurrentRoomId;

        if (!_worldMap.TryMove(session.CurrentRoomId, direction, out RoomId roomId) || roomId == default)
        {
            _logger.LogInformation("Can not move {Direcion} in {RoomId}", direction, session.CurrentRoomId.Value);
            await _protocolMessageSender.SendAsync(
                context.ConnectionId,
                ProtocolMessageTypes.Error,
                new ErrorData($"You can not go {direction} from here."),
                ct);
            return;
        }

        if (!_sessionManager.TryUpdateRoom(session.ConnectionId, roomId))
        {
            _logger.LogError("Unable to update room for {ConnectionId} to {RoomId}", session.ConnectionId, roomId);
            return;
        }

        // Broadcast leave
        var previousRoomConnectionIds = _sessionManager
            .GetByRoom(previousRoomId)
            .Where(x => x.ConnectionId != session.ConnectionId)
            .Select(x => x.ConnectionId)
            .ToList();
        await _protocolMessageSender.BroadcastAsync(
            previousRoomConnectionIds,
            ProtocolMessageTypes.UserLeft,
            new UserPresenceData(session.Username),
            ct);

        // Broadcast enter
        var currentRoomConnectionIds = _sessionManager
            .GetByRoom(roomId)
            .Where(x => x.ConnectionId != session.ConnectionId)
            .Select(x => x.ConnectionId)
            .ToList();
        await _protocolMessageSender.BroadcastAsync(
            currentRoomConnectionIds,
            ProtocolMessageTypes.UserEntered,
            new UserPresenceData(session.Username),
            ct);

        // Send message to user
        if (!_worldMap.TryGetRoom(roomId, out var room) || room is null)
        {
            _logger.LogError("Unable to get room for {RoomId}", roomId);
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
