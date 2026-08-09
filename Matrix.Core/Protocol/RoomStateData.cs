namespace Matrix.Core.Protocol;

public sealed record RoomStateData(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> Users,
    IReadOnlyList<string> Exits);
