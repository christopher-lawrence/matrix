using Matrix.Core.Protocol;

namespace Matrix.Server.Services;

public interface IProtocolMessageSender
{
    Task<bool> SendAsync(Guid connectionId, string type, object? data, CancellationToken ct = default);
    Task BroadcastAsync(IEnumerable<Guid> connectionIds, string type, object? data, CancellationToken ct = default);
}

public sealed class ProtocolMessageSender : IProtocolMessageSender
{
    private readonly IConnectionManager _connectionManager;

    public ProtocolMessageSender(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    public async Task<bool> SendAsync(Guid connectionId, string type, object? data, CancellationToken ct = default)
    {
        var json = ProtocolJson.Serialize(new ServerMessage(type, data));
        return await _connectionManager.SendTextAsync(connectionId, json, ct);
    }

    public async Task BroadcastAsync(IEnumerable<Guid> connectionIds, string type, object? data, CancellationToken ct = default)
    {
        var json = ProtocolJson.Serialize(new ServerMessage(type, data));
        await _connectionManager.BroadcastTextAsync(connectionIds, json, ct);
    }
}
