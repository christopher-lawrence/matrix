using System.Text.Json;
using Matrix.Core.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace Matrix.Server.Services.Commands;

public sealed class HelpCommand : ICommand
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProtocolMessageSender _protocolMessageSender;

    public HelpCommand(IServiceProvider serviceProvider, IProtocolMessageSender protocolMessageSender)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _protocolMessageSender = protocolMessageSender ?? throw new ArgumentNullException(nameof(protocolMessageSender));
    }

    public string Type => ProtocolMessageTypes.Help;

    public string Description => "Lists available commands and examples.";

    public string Example => "help";

    public async Task ExecuteAsync(CommandContext context, JsonElement? args, CancellationToken ct)
    {
        var commands = _serviceProvider
            .GetServices<ICommand>()
            .OrderBy(command => command.Type, StringComparer.OrdinalIgnoreCase)
            .Select(command => new HelpCommandData(command.Type, command.Description, command.Example))
            .ToList();

        await _protocolMessageSender.SendAsync(
            context.ConnectionId,
            ProtocolMessageTypes.Help,
            new HelpData(commands),
            ct);
    }
}
