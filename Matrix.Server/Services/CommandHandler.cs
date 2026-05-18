using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Matrix.Core.Services;
using Matrix.Server.Extensions;

namespace Matrix.Server.Services;

public interface ICommandHandler
{
    Task HandleMessage(string message, Guid connectionId, WebSocket socket, CancellationToken ct);
}

public sealed class CommandHandler : ICommandHandler
{
    private readonly ILogger<CommandHandler> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;

    public CommandHandler(
        ILogger<CommandHandler> logger, ISessionManager sessionManager, WorldMap worldMap)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _worldMap = worldMap ?? throw new ArgumentNullException(nameof(worldMap));
    }

    public async Task HandleMessage(string message, Guid connectionId, WebSocket socket, CancellationToken ct)
    {
        if (!IsValidCommand(message))
        {
            await socket.SendTextAsync("Invalid command", _logger, ct);
            return;
        }

        var (command, parameters) = ParseMessage(message);

        if (command is null)
        {
            _logger.LogInformation("Command is null");
            return;
        }

        await HandleCommandAsync(command, parameters, connectionId, socket, ct);
    }

    private Task HandleCommandAsync(string command, string? parameters, Guid connectionId, WebSocket socket, CancellationToken ct)
    {
        if (!_sessionManager.TryGetByConnectionId(connectionId, out SessionState? session) || session is null)
        {
            _logger.LogError("Unable to find session for {ConnectionId}", connectionId);
            return Task.CompletedTask;
        }

        return command switch
        {
            "/look" => HandleLookCommandAsync(session.Value, socket, ct),
            "/who" => HandleWhoCommandAsync(session.Value, socket, ct),
            _ => HandleUnknownCommand(command, socket, ct)
        };
    }

    private async Task HandleWhoCommandAsync(SessionState session, WebSocket socket, CancellationToken ct)
    {
        if (!_worldMap.TryGetRoom(session.CurrentRoomId, out var room) || room is null)
        {
            _logger.LogError("Unable to get room for id {CurrentRoomId}", session.CurrentRoomId);
            return;
        }
        var sessions = _sessionManager.GetByRoom(room.Id);
        var users = sessions
            .Where(x => !string.IsNullOrEmpty(x.Username))
            .Select(x => x.Username)
            .OrderBy(x => x);
        var usersMessage = users.Count() > 1
            ? $"Users here: {string.Join(", ", users)}"
            : "You are alone in this room.";

        var sb = new StringBuilder();
        sb.AppendLine(usersMessage);

        await socket.SendTextAsync(sb.ToString(), _logger, ct);
    }

    private async Task HandleLookCommandAsync(SessionState session, WebSocket socket, CancellationToken ct)
    {
        if (!_worldMap.TryGetRoom(session.CurrentRoomId, out var room) || room is null)
        {
            _logger.LogError("Unable to get room for id {CurrentRoomId}", session.CurrentRoomId);
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

        await socket.SendTextAsync(sb.ToString(), _logger, ct);
    }

    private bool IsValidCommand(string message) => message.TrimStart().StartsWith('/');

    private (string? command, string? parameters) ParseMessage(string message)
    {
        var matcher = Regex.Match(message, @"^\/\w+");

        if (!matcher.Success)
        {
            return (null, null);
        }

        var command = matcher.Value;
        var parameters = message.AsSpan(command.Length).TrimStart();

        return (command, parameters.ToString());
    }

    private Task HandleUnknownCommand(string command, WebSocket socket, CancellationToken ct)
        => socket.SendTextAsync($"Unknown command: {command}", _logger, ct);
}
