using Matrix.Core.Protocol;

namespace Matrix.Server.Services;

public interface ICommandHandler
{
    Task HandleMessage(string message, Guid connectionId, CancellationToken ct);
}

public sealed class CommandHandler : ICommandHandler
{
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly Dictionary<string, ICommand> _commands;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(
        IProtocolMessageSender protocolMessageSender,
        IEnumerable<ICommand> commands,
        ILogger<CommandHandler> logger)
    {
        _protocolMessageSender = protocolMessageSender ?? throw new ArgumentNullException(nameof(protocolMessageSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _commands = commands.ToDictionary(x => x.Type, StringComparer.OrdinalIgnoreCase);
    }

    public async Task HandleMessage(string message, Guid connectionId, CancellationToken ct)
    {
        if (!ProtocolJson.TryDeserializeClientMessage(message, out var clientMessage) || clientMessage is null)
        {
            _logger.LogWarning("Invalid protocol JSON from {ConnectionId}", connectionId);
            await _protocolMessageSender.SendAsync(
                connectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("Invalid JSON."),
                ct);
            return;
        }

        var commandType = clientMessage.Type.Trim();
        if (!_commands.TryGetValue(commandType, out var handler))
        {
            _logger.LogWarning(
                "Unknown protocol message type with length {TypeLength} from {ConnectionId}",
                commandType.Length,
                connectionId);
            await _protocolMessageSender.SendAsync(
                connectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("Unknown command."),
                ct);
            return;
        }

        try
        {
            await handler.ExecuteAsync(new CommandContext(connectionId), clientMessage.Args, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Command {CommandType} canceled for {ConnectionId}",
                commandType,
                connectionId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Command {CommandType} failed for {ConnectionId}",
                commandType,
                connectionId);
            await _protocolMessageSender.SendAsync(
                connectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("Command failed. Please try again."),
                ct);
        }
    }
}
