namespace VAAI.Shared.Communication;

public static class Broadcasts
{
    public const string SessionConnect = "SessionConnect";
    public const string OtherSessionConnect = "OtherSessionConnect";

    public const string SpeechToText = "SpeechToText";
    public const string TextToSpeech = "TextToSpeech";
    public const string TextToText = "TextToText";

    public const string SpeechToTextResult = "SpeechToTextResult";
    public const string TextToSpeechResult = "TextToSpeechResult";
    public const string TextToTextResult = "TextToTextResult";

    public const string ServerError = "ServerError";
}
