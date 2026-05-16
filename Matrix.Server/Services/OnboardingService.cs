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

            var buffer = new byte[1024 * 4];
            var results = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            // FOLLOWUP: we should probably handle this better
            // -- this will atleast fail the onboarding and close the connection
            if (results.MessageType == WebSocketMessageType.Close)
            {
                _logger.LogInformation("Received Close message while getting username");
                return null;
            }

            if (results.MessageType == WebSocketMessageType.Binary)
            {
                _logger.LogInformation("Received Binary message while getting username");
                return null;
            }

            if (!TryValidateMessage(buffer[0..results.Count], out var username, 24))
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

    private bool TryValidateMessage(ArraySegment<byte> buffer, out string? cleanBuffer, int? maxLength = null)
    {
        cleanBuffer = null;

        if (buffer.Array == null || buffer.Count == 0)
        {
            return false;
        }

        cleanBuffer = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count).Trim();

        if (cleanBuffer.Length == 0)
        {
            cleanBuffer = null;
            return false;
        }

        if (maxLength is not null && cleanBuffer.Length > maxLength)
        {
            cleanBuffer = null;
            return false;
        }

        return true;
    }
}
