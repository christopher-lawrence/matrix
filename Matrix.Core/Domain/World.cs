using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public sealed class World
{
    private readonly IReadOnlyDictionary<AreaId, Area> _areas;

    public WorldId Id { get; }
    public string Name { get; }
    public AreaId DefaultAreaId { get; }

    public World()
    {
        Id = new WorldId(Guid.NewGuid());
        Name = "The Matrix";

        var lobbyId = new AreaId(Guid.NewGuid());
        var arcadeId = new AreaId(Guid.NewGuid());

        DefaultAreaId = lobbyId;

        var lobby = new Area
        {
            Id = lobbyId,
            Name = "Lobby",
            Description = "Welcome to the Lobby",
            WorldId = Id,
            Connections =
            [
                new AreaConnection
                {
                    Direction = Direction.North,
                    Destination = arcadeId
                }
            ]
        };
        var arcade = new Area
        {
            Id = arcadeId,
            Name = "Arcade",
            Description = "Enjoy the arcade",
            WorldId = Id,
            Connections =
            [
                new AreaConnection
                {
                    Direction = Direction.South,
                    Destination = lobbyId
                }
            ]
        };

        _areas = new Dictionary<AreaId, Area>
        {
            { lobbyId, lobby },
            { arcadeId, arcade }
        };
    }

    public Area GetDefaultArea() => _areas[DefaultAreaId];

    public bool TryGetArea(AreaId areaId, out Area? area)
        => _areas.TryGetValue(areaId, out area);

    public bool TryMove(AreaId fromAreaId, Direction direction, out AreaId destinationAreaId)
    {
        destinationAreaId = default;

        if (!TryGetArea(fromAreaId, out Area? fromArea) || fromArea is null)
        {
            return false;
        }

        var connection = fromArea.Connections.FirstOrDefault(x => x.Direction == direction);
        if (connection is null)
        {
            return false;
        }

        destinationAreaId = connection.Destination;
        return true;
    }
}
