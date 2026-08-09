namespace Matrix.Core.Protocol;

public static class ProtocolMessageTypes
{
    public const string Look = "look";
    public const string Who = "who";
    public const string Move = "move";
    public const string Say = "say";
    public const string Help = "help";
    public const string SetUsername = "setUsername";

    public const string RoomState = "roomState";
    public const string ChatMessage = "chatMessage";
    public const string UserEntered = "userEntered";
    public const string UserLeft = "userLeft";
    public const string Prompt = "prompt";
    public const string Error = "error";
}
