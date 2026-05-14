using Matrix.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<WebSocketConnectionService>();

var app = builder.Build();
app.UseWebSockets();
app.MapControllers();

app.Run();

