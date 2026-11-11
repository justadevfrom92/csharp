namespace SignalRChat.Constants;

public static class ChatRuleConstants
{
    public const int MaximumMessageLength = 200;
    public const string RoomNamePattern = "^[a-z0-9-]{1,20}$"; // e.g. "general", "dotnet-10"
}
