using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public sealed class AreaConnection
{
    public required AreaId Destination { get; init; }
    public required Direction Direction { get; init; }
}
