using Matrix.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Server.Controllers;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly WebSocketConnectionService _connectionService;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        WebSocketConnectionService connectionService,
        ILogger<WebSocketController> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task Get()
    {
        _logger.LogDebug("Received connection...");

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _connectionService.AddConnectionAsync(Guid.NewGuid(), webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await HttpContext.Response.WriteAsync("Use a WebSocket client to connect to /ws");
        }

    }
}
