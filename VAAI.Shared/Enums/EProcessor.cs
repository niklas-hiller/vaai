using VAAI.Shared.Communication;

namespace VAAI.Shared.Enums;

public enum EProcessor
{
    TTS,
    STT,
    LLM,
}
public static class EProcessorExtensions
{
    public static string ToGroup(this EProcessor processor) => processor switch
    {
        EProcessor.TTS => SessionGroups.TTS_AI,
        EProcessor.STT => SessionGroups.STT_AI,
        EProcessor.LLM => SessionGroups.LLM_AI,
        _ => throw new ArgumentOutOfRangeException(nameof(processor), $"Not expected processor value: {processor}")
    };

    public static string ToIngoingBroadcast(this EProcessor processor) => processor switch
    {
        EProcessor.TTS => Broadcasts.TextToSpeech,
        EProcessor.STT => Broadcasts.SpeechToText,
        EProcessor.LLM => Broadcasts.TextToText,
        _ => throw new ArgumentOutOfRangeException(nameof(processor), $"Not expected processor value: {processor}")
    };

    public static string ToOutgoingBroadcast(this EProcessor processor) => processor switch
    {
        EProcessor.TTS => Broadcasts.TextToSpeechResult,
        EProcessor.STT => Broadcasts.SpeechToTextResult,
        EProcessor.LLM => Broadcasts.TextToTextResult,
        _ => throw new ArgumentOutOfRangeException(nameof(processor), $"Not expected processor value: {processor}")
    };
}