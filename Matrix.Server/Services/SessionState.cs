using Matrix.Core.Ids;

namespace Matrix.Server.Services;

public readonly record struct SessionState
{
    public Guid ConnectionId { get; init; }
    public UserSessionId SessionId { get; init; }
    public PlayerId PlayerId { get; init; }
    public string Username { get; init; }
    public RoomId CurrentRoomId { get; init; }
}
