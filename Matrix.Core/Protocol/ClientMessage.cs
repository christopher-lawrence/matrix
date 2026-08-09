using System.Text.Json;

namespace Matrix.Core.Protocol;

public sealed record ClientMessage(string Type, JsonElement? Args = null);
