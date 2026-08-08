using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Server.Services.Commands;

public sealed class HelpCommand : ICommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionManager _connectionManager;

    public HelpCommand(IServiceProvider serviceProvider, IConnectionManager connectionManager)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    public string Name => "/help";

    public string Description => "Lists available commands and examples.";

    public string Example => "/help";

    public async Task ExecuteAsync(CommandContext context, string? parameters, CancellationToken ct)
    {
        var commands = _serviceProvider
            .GetServices<ICommand>()
            .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Available commands:");

        foreach (var command in commands)
        {
            sb.AppendLine($"{command.Name} - {command.Description}");
            sb.AppendLine($"  Example: {command.Example}");
        }

        await _connectionManager.SendTextAsync(context.ConnectionId, sb.ToString(), ct);
    }
}
