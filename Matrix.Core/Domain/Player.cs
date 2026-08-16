using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public class Player
{
    public required PlayerId Id { get; init; }
    public required string Name { get; set; }

    // relationships
    public AreaId? CurrentAreaId { get; set; }
}
