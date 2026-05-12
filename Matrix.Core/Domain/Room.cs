using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public sealed class Room
{
    public required RoomId Id { get; init; }
    public required string Name { get; set; }
    public string Description { get; set; }
    public IReadOnlyDictionary<Direction, RoomId> Exits { get; set; }

    // relationships
    public required WorldId WorldId { get; set; }

    public Room()
    {
        Description = "";
        Exits = new Dictionary<Direction, RoomId>();
    }
}
