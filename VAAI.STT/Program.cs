using Whisper.net.Ggml;

namespace VAAI.STT;

internal class Program
{
    static async Task Main(string[] args)
        => await new WhisperClient(GgmlType.BaseEn, "en", 8).StartAsync();
}