using System.Text.Json;

namespace Matrix.Server.Services;

public readonly record struct CommandContext(Guid ConnectionId);

public interface ICommand
{
    string Type { get; }
    string Description { get; }
    string Example { get; }
    Task ExecuteAsync(CommandContext context, JsonElement? args, CancellationToken ct);
}
