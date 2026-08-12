using System.Text.Json;
using Matrix.Core.Protocol;

namespace Matrix.Server.Services.Commands;

public sealed class SayCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly IProtocolMessageSender _protocolMessageSender;
    private readonly ILogger<SayCommand> _logger;

    public SayCommand(
        ISessionManager sessionManager,
        IProtocolMessageSender protocolMessageSender,
        ILogger<SayCommand> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _protocolMessageSender = protocolMessageSender ?? throw new ArgumentNullException(nameof(protocolMessageSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Type => ProtocolMessageTypes.Say;

    public string Description => "Broadcasts a message to users in your current area.";

    public string Example => "say hello";

    public async Task ExecuteAsync(CommandContext context, JsonElement? args, CancellationToken ct)
    {
        if (!_sessionManager.TryGetByConnectionId(context.ConnectionId, out SessionState? session) || session is null)
        {
            _logger.LogError("Unable to find session for {ConnectionId}", context.ConnectionId);
            return;
        }

        var sayArgs = ProtocolJson.DeserializeArgs<SayArgs>(args);
        var message = sayArgs?.Message?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            await _protocolMessageSender.SendAsync(
                context.ConnectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("You must provide a message to say."),
                ct);
            return;
        }

        var areaConnectionIds = _sessionManager
            .GetByArea(session.Value.CurrentAreaId)
            .Select(x => x.ConnectionId)
            .ToList();

        if (areaConnectionIds.Count == 0)
        {
            _logger.LogWarning(
                "No area recipients found for {ConnectionId} in {AreaId}",
                context.ConnectionId,
                session.Value.CurrentAreaId);
            await _protocolMessageSender.SendAsync(
                context.ConnectionId,
                ProtocolMessageTypes.Error,
                new ErrorData("No one can hear you right now."),
                ct);
            return;
        }

        await _protocolMessageSender.BroadcastAsync(
            areaConnectionIds,
            ProtocolMessageTypes.ChatMessage,
            new ChatMessageData(session.Value.Username, message),
            ct);
    }
}
