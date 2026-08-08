
using System.Text;
using System.Text.RegularExpressions;
using Matrix.Core.Domain;
using Matrix.Core.Ids;
using Matrix.Core.Services;

namespace Matrix.Server.Services.Commands;

public sealed class GoCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<GoCommand> _logger;

    public GoCommand(
        ISessionManager sessionManager,
        WorldMap worldMap,
        IConnectionManager connectionManager,
        ILogger<GoCommand> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _worldMap = worldMap ?? throw new ArgumentNullException(nameof(worldMap));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "/go";

    public string Description => "Moves to an adjacent room by direction.";

    public string Example => "/go north";

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

        if (parameters is null)
        {
            _logger.LogError("No direction specified for {Command}", Name);
            // FOLLOWUP: make ICommand have a GetHelp method
            await _connectionManager.SendTextAsync(
                context.ConnectionId, "You must specify a direction: north, south, east, west", ct);
            return;
        }

        var allParameters = parameters.Trim().Split(' ');
        if (allParameters.Length == 0)
        {
            _logger.LogError("No direction could be parsed for {Command}", Name);
            // FOLLOWUP: make ICommand have a GetHelp method
            await _connectionManager.SendTextAsync(
                context.ConnectionId, "You must specify a direction: north, south, east, west", ct);
            return;
        }

        if (!TryParseDirection(allParameters[0], out Direction? direction) || direction is null)
        {
            _logger.LogWarning("Unable to parse direction for {Command} from {ConnectionId}", Name, context.ConnectionId);
            // FOLLOWUP: make ICommand have a GetHelp method
            await _connectionManager.SendTextAsync(
                context.ConnectionId, $"Invalid direction: north, south, east, west", ct);
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
            await _connectionManager.SendTextAsync(context.ConnectionId, $"You can not go {direction} from here.", ct);
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
        await _connectionManager.BroadcastTextAsync(previousRoomConnectionIds, $"{session.Username} left {direction}.");

        // Broadcast enter
        var currentRoomConnectionIds = _sessionManager
            .GetByRoom(roomId)
            .Where(x => x.ConnectionId != session.ConnectionId)
            .Select(x => x.ConnectionId)
            .ToList();
        await _connectionManager.BroadcastTextAsync(currentRoomConnectionIds, $"{session.Username} entered the room");

        // Send message to user
        if (!_worldMap.TryGetRoom(roomId, out var room) || room is null)
        {
            _logger.LogError("Unable to get room for {RoomId}", roomId);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"You moved {direction} to {room.Name}");
        sb.AppendLine("Use /look to inspect room.");
        await _connectionManager.SendTextAsync(context.ConnectionId, sb.ToString());
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
