using System.Net.WebSockets;
using Matrix.Core.Services;
using Matrix.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.Server.Controllers;

[ApiController]
[Route("ws")]
public class WebSocketController : ControllerBase
{
    private readonly WebSocketConnectionService _connectionService;
    private readonly ISessionManager _sessionManager;
    private readonly WorldMap _worldMap;
    private readonly ILogger<WebSocketController> _logger;

    public WebSocketController(
        WebSocketConnectionService connectionService,
        ISessionManager sessionManager,
        WorldMap worldMap,
        ILogger<WebSocketController> logger)
    {
        _connectionService = connectionService;
        _sessionManager = sessionManager;
        _worldMap = worldMap;
        _logger = logger;
    }

    [HttpGet]
    public async Task Get()
    {
        _logger.LogDebug("Received connection...");

        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            // webSocket is disposed in WebSocketConnectionService
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var connectionId = Guid.NewGuid();
            var sessionState = new SessionState
            {
                ConnectionId = connectionId,
                PlayerId = new Core.Ids.PlayerId(Guid.NewGuid()),
                SessionId = new Core.Ids.UserSessionId(Guid.NewGuid()),
                Username = "anonymous",
                CurrentRoomId = _worldMap.DefaultRoomId,
            };

            if (!_sessionManager.TryAdd(sessionState))
            {
                _logger.LogWarning("Failed to add session {SessionId}", sessionState.SessionId);
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, "Closing time", CancellationToken.None);
                return;
            }

            await _connectionService.AddConnectionAsync(connectionId, webSocket);

        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await HttpContext.Response.WriteAsync("Use a WebSocket client to connect to /ws");
        }

    }
}
