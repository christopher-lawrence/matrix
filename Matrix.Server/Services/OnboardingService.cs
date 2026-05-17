using System.Net.WebSockets;
using System.Text;
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
            await socket.SendTextAsync("Enter username: ", _logger, ct);

            var message = await socket.ReceiveMessageAsync(_logger, ct);

            if (message is null)
            {
                return null;
            }

            if (!TryValidateMessage(message, out var username, 24))
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

    private bool TryValidateMessage(string message, out string? cleanMessage, int? maxLength = null)
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
