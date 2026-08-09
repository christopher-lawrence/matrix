using System.Text.Json;

namespace Matrix.Core.Protocol;

public static class ProtocolJson
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static bool TryDeserializeClientMessage(string json, out ClientMessage? message)
    {
        message = null;

        try
        {
            message = JsonSerializer.Deserialize<ClientMessage>(json, SerializerOptions);
            return message is not null && !string.IsNullOrWhiteSpace(message.Type);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static T? DeserializeArgs<T>(JsonElement? args)
    {
        if (args is null || args.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return default;
        }

        return args.Value.Deserialize<T>(SerializerOptions);
    }

    public static string Serialize(ServerMessage message)
    {
        return JsonSerializer.Serialize(message, SerializerOptions);
    }
}
