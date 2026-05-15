using Matrix.Core.Services;
using Matrix.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<WebSocketConnectionService>();
builder.Services.AddSingleton<ISessionManager, SessionManager>();
builder.Services.AddSingleton<WorldMap>();

var app = builder.Build();
app.UseWebSockets();
app.MapControllers();

app.Run();

