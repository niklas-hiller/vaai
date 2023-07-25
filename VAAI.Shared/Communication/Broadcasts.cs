namespace VAAI.Shared.Communication;

public static class Broadcasts
{
    // Session Establish
    public const string SessionConnect = "SessionConnect";

    // Invoker -> AI
    public const string SpeechToText = "SpeechToText";
    public const string TextToSpeech = "TextToSpeech";
    public const string TextToText = "TextToText";

    // AI -> Listener
    public const string SpeechToTextResult = "SpeechToTextResult";
    public const string TextToSpeechResult = "TextToSpeechResult";
    public const string TextToTextResult = "TextToTextResult";
}
