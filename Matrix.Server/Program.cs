using Matrix.Core.Services;
using Matrix.Server.Services;
using Matrix.Server.Services.Commands;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<WebSocketConnectionService>();
builder.Services.AddSingleton<ISessionManager, SessionManager>();
builder.Services.AddSingleton<WorldMap>();
builder.Services.AddSingleton<IOnboardingService, OnboardingService>();
builder.Services.AddSingleton<ICommandHandler, CommandHandler>();
builder.Services.AddSingleton<ICommand, LookCommand>();
builder.Services.AddSingleton<ICommand, WhoCommand>();
builder.Services.AddSingleton<ICommand, GoCommand>();
builder.Services.AddSingleton<ICommand, HelpCommand>();

var app = builder.Build();
app.UseWebSockets();
app.MapControllers();

app.Run();
