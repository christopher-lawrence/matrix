namespace Matrix.Server.Services;

public readonly record struct CommandContext(Guid ConnectionId);

public interface ICommand
{
    string Name { get; }
    Task ExecuteAsync(CommandContext context, string? parameters, CancellationToken ct);
}
