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
            Exits = new Dictionary<Direction, AreaId> { { Direction.North, arcadeId } }
        };
        var arcade = new Area
        {
            Id = arcadeId,
            Name = "Arcade",
            Description = "Enjoy the arcade",
            WorldId = Id,
            Exits = new Dictionary<Direction, AreaId> { { Direction.South, lobbyId } }
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

        if (!fromArea.Exits.TryGetValue(direction, out destinationAreaId))
        {
            return false;
        }

        return true;
    }
}
