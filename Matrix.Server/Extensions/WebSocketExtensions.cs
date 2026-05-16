using System.Net.WebSockets;
using System.Text;

namespace Matrix.Server.Extensions;

public static class WebSocketExtensions
{
    public static async Task SendTextAsync<T>(this WebSocket socket, string message, ILogger<T> logger, CancellationToken ct)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to socket");
        }
    }

}
