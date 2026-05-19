using System.Text.RegularExpressions;

namespace Matrix.Server.Services;

public interface ICommandHandler
{
    Task HandleMessage(string message, Guid connectionId, CancellationToken ct);
}

public sealed class CommandHandler : ICommandHandler
{
    private readonly IConnectionManager _connectionManager;
    private readonly Dictionary<string, ICommand> _commands;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(
        IConnectionManager connectionManager,
        IEnumerable<ICommand> commands,
        ILogger<CommandHandler> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
        _commands = commands.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public async Task HandleMessage(string message, Guid connectionId, CancellationToken ct)
    {
        if (!IsValidCommand(message))
        {
            await _connectionManager.SendTextAsync(connectionId, "Invalid command", ct);
            return;
        }

        var (command, parameters) = ParseMessage(message);
        if (command is null)
        {
            _logger.LogInformation("Command is null");
            return;
        }

        if (!_commands.TryGetValue(command, out var handler))
        {
            await _connectionManager.SendTextAsync(connectionId, $"Unknown command: {command}", ct);
            return;
        }

        await handler.ExecuteAsync(new CommandContext(connectionId), parameters, ct);
    }

    private static bool IsValidCommand(string message) => message.TrimStart().StartsWith('/');

    private static (string? command, string? parameters) ParseMessage(string message)
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
}
