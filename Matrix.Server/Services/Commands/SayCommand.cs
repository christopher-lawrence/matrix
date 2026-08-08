namespace Matrix.Server.Services.Commands;

public sealed class SayCommand : ICommand
{
    private readonly ISessionManager _sessionManager;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<SayCommand> _logger;

    public SayCommand(
        ISessionManager sessionManager,
        IConnectionManager connectionManager,
        ILogger<SayCommand> logger)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "/say";

    public string Description => "Broadcasts a message to users in your current room.";

    public string Example => "/say hello";

    public async Task ExecuteAsync(CommandContext context, string? parameters, CancellationToken ct)
    {
        if (!_sessionManager.TryGetByConnectionId(context.ConnectionId, out SessionState? session) || session is null)
        {
            _logger.LogError("Unable to find session for {ConnectionId}", context.ConnectionId);
            return;
        }

        var message = parameters?.Trim();
        if (string.IsNullOrEmpty(message))
        {
            await _connectionManager.SendTextAsync(context.ConnectionId, "You must provide a message to say.", ct);
            return;
        }

        var roomConnectionIds = _sessionManager
            .GetByRoom(session.Value.CurrentRoomId)
            .Select(x => x.ConnectionId)
            .ToList();

        if (roomConnectionIds.Count == 0)
        {
            _logger.LogWarning(
                "No room recipients found for {ConnectionId} in {RoomId}",
                context.ConnectionId,
                session.Value.CurrentRoomId);
            await _connectionManager.SendTextAsync(context.ConnectionId, "No one can hear you right now.", ct);
            return;
        }

        await _connectionManager.BroadcastTextAsync(
            roomConnectionIds,
            $"{session.Value.Username} says: {message}",
            ct);
    }
}
