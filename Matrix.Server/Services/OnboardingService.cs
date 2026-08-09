using System.Net.WebSockets;
using System.Text;
using Matrix.Core.Protocol;
using Matrix.Server.Extensions;

namespace Matrix.Server.Services;

public interface IOnboardingService
{
    Task<string?> GetUsernameAsync(WebSocket socket, CancellationToken ct = default);
}

public sealed class OnboardingService : IOnboardingService
{
    private readonly ILogger<OnboardingService> _logger;

    public OnboardingService(ILogger<OnboardingService> logger)
    {
        _logger = logger;
    }

    public async Task<string?> GetUsernameAsync(WebSocket socket, CancellationToken ct = default)
    {
        if (socket.CloseStatus.HasValue)
        {
            return null;
        }

        try
        {
            var prompt = ProtocolJson.Serialize(
                new ServerMessage(ProtocolMessageTypes.Prompt, new PromptData("Enter username:")));
            await socket.SendTextAsync(prompt, _logger, ct);

            var message = await socket.ReceiveMessageAsync(_logger, ct);

            if (message is null)
            {
                return null;
            }

            if (!ProtocolJson.TryDeserializeClientMessage(message, out var clientMessage)
                || clientMessage is null
                || !clientMessage.Type.Equals(ProtocolMessageTypes.SetUsername, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var usernameArgs = ProtocolJson.DeserializeArgs<UsernameArgs>(clientMessage.Args);
            if (!TryValidateMessage(usernameArgs?.Username, out var username, 24))
            {
                return null;
            }

            return username;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while onboarding username");
        }

        return null;
    }

    private bool TryValidateMessage(string? message, out string? cleanMessage, int? maxLength = null)
    {
        cleanMessage = null;

        if (message is null || message.Length == 0)
        {
            return false;
        }

        cleanMessage = message.Trim();

        if (cleanMessage.Length == 0)
        {
            cleanMessage = null;
            return false;
        }

        if (maxLength is not null && cleanMessage.Length > maxLength)
        {
            cleanMessage = null;
            return false;
        }

        return true;
    }
}
