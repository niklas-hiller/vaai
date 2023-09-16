namespace VAAI.Shared.Communication;

public class SessionGroups
{
    public const string TTS_AI = "TTS_AI";
    public const string STT_AI = "STT_AI";
    public const string LLM_AI = "LLM_AI";

    public const string Invoker = "Invoker";
    public const string Listener = "Listener";

    public static bool Contains(string groupName) 
        => typeof(SessionGroups).GetField(groupName) != null;
}
