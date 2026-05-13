using Matrix.Core.Ids;

namespace Matrix.Core.Domain;

public sealed class UserSession
{
    public required UserSessionId Id { get; init; }

    // relationships
    public required PlayerId PlayerId { get; init; }
}
