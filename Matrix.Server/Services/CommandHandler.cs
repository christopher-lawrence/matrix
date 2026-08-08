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
            _logger.LogWarning("Invalid command input from {ConnectionId}", connectionId);
            await _connectionManager.SendTextAsync(connectionId, "Invalid command", ct);
            return;
        }

        var (command, parameters) = ParseMessage(message);
        if (command is null)
        {
            _logger.LogWarning("Unable to parse command from {ConnectionId}", connectionId);
            return;
        }

        if (!_commands.TryGetValue(command, out var handler))
        {
            _logger.LogWarning(
                "Unknown command {Command} from {ConnectionId}",
                command,
                connectionId);
            await _connectionManager.SendTextAsync(connectionId, $"Unknown command: {command}. Use /help to see available commands.", ct);
            return;
        }

        try
        {
            await handler.ExecuteAsync(new CommandContext(connectionId), parameters, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Command {Command} canceled for {ConnectionId}",
                command,
                connectionId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Command {Command} failed for {ConnectionId}",
                command,
                connectionId);
            await _connectionManager.SendTextAsync(
                connectionId,
                "Command failed. Please try again.",
                ct);
        }
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
