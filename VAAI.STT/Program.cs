using VAAI.ClientLibrary;
using Whisper.net;
using Whisper.net.Ggml;

namespace VAAI.STT;

internal class Program
{
    static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
        using var fileWriter = File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }

    static async Task Main(string[] args)
    {
        string path = "ggml-baseen.bin";
        if (!File.Exists(path))
        {
            await DownloadModel(path, GgmlType.BaseEn);
        }

        var whisperFactory = WhisperFactory.FromPath(path);

        var processor = whisperFactory.CreateBuilder()
            .WithLanguage("en").WithThreads(16)
            .Build();

        var client = new Client("Whisper");
        client.registerSTT(async (message) =>
        {
            await foreach (var result in processor.ProcessAsync(message.Content))
            {
                Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
            }
            return "";
        });
    }
}