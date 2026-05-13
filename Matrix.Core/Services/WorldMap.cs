using Matrix.Core.Domain;
using Matrix.Core.Ids;

namespace Matrix.Core.Services;

public sealed class WorldMap
{
    private readonly WorldId _worldId;
    private readonly IReadOnlyDictionary<RoomId, Room> _rooms;

    public RoomId DefaultRoomId { get; }

    public WorldMap()
    {
        _worldId = new WorldId(Guid.NewGuid());
        var lobbyId = new RoomId(Guid.NewGuid());
        var arcadeId = new RoomId(Guid.NewGuid());

        DefaultRoomId = lobbyId;

        var world = new World
        {
            Id = _worldId,
            Name = "The Matrix",
        };
        var lobby = new Room
        {
            Id = lobbyId,
            Name = "Lobby",
            Description = "Welcome to the Lobby",
            WorldId = world.Id,
            Exits = new Dictionary<Direction, RoomId> { { Direction.North, arcadeId } }
        };
        var arcade = new Room
        {
            Id = arcadeId,
            Name = "Arcade",
            Description = "Enjoy the arcade",
            WorldId = world.Id,
            Exits = new Dictionary<Direction, RoomId> { { Direction.South, lobbyId } }
        };

        _rooms = new Dictionary<RoomId, Room> {
            { lobbyId, lobby },
            { arcadeId, arcade }
        };
    }

    public Room GetDefaultRoom() => _rooms[DefaultRoomId];

    public bool TryGetRoom(RoomId roomId, out Room? room)
        => _rooms.TryGetValue(roomId, out room);

    public bool TryMove(RoomId fromRoomId, Direction direction, out RoomId destinationRoomId)
    {
        destinationRoomId = default;

        if (!TryGetRoom(fromRoomId, out Room? fromRoom) || fromRoom is null)
        {
            return false;
        }

        if (!fromRoom.Exits.TryGetValue(direction, out destinationRoomId))
        {
            return false;
        }

        return true;
    }
}
