using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public sealed class World
{
    public required WorldId Id { get; init; }
    public required string Name { get; set; }
}
