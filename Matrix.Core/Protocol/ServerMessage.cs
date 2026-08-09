namespace Matrix.Core.Protocol;

public sealed record ServerMessage(string Type, object? Data = null);
