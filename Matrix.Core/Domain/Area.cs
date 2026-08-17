using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public sealed class Area
{
    public required AreaId Id { get; init; }
    public required string Name { get; set; }
    public string Description { get; set; }
    public IReadOnlyCollection<AreaConnection> Connections { get; init; }

    // relationships
    public required WorldId WorldId { get; init; }

    public Area()
    {
        Description = "";
        Connections = [];
    }
}
