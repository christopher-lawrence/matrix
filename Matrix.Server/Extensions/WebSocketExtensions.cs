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

    public static async Task<string?> ReceiveMessageAsync<T>(
        this WebSocket socket, ILogger<T> logger, CancellationToken ct)
    {
        var sb = new StringBuilder();
        WebSocketReceiveResult results;

        try
        {
            do
            {
                var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
                results = await socket.ReceiveAsync(buffer, ct);

                // FOLLOWUP: we should probably handle this better
                // -- this will atleast fail the onboarding and close the connection
                if (results.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("Received Close message");
                    return null;
                }

                if (results.MessageType == WebSocketMessageType.Binary)
                {
                    logger.LogInformation("Received Binary message");
                    return null;
                }

                if (buffer.Array is null)
                {
                    return null;
                }

                var message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, results.Count);

                sb.Append(message);
            } while (!results.EndOfMessage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception while receiving message");
        }

        return sb.ToString();
    }

}
