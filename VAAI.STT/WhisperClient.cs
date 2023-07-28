using System.Diagnostics;
using VAAI.Library;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;
using Whisper.net;
using Whisper.net.Ggml;

namespace VAAI.STT;

internal class WhisperClient
{
    private const string CLIENT_NAME = "Whisper";

    // Samples per second. Should always be 16.000 to work with whisper.
    private const int SAMPLE_RATE = 16000;
    // The minimum noise that is considered "something" within 16.000 Samples.
    private const float MINIMUM_NOISE = 50;
    // At how many samples without noise the segment should stop.
    private const int END_AT = 9600;
    // Minimum relevant samples
    private const int MINIMUM_RELEVANT = 9600;
    // How much "unrelevant" content at start and end should stay (if bigger than END_AT, will be equal END_AT)
    private const int KEEP_PUFFER = 3200;

    private readonly string Language;
    private readonly int Threads;
    private readonly WhisperProcessor Processor;

    private readonly List<float[]> RetainedData = new();
    private int CurrentSamplesCount = 0;
    private int LastNoise = 0;

    private static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
        using var fileWriter = File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }

    internal WhisperClient(GgmlType ggmlType, string language, int threads)
    {
        string modelName = Enum.GetNames(typeof(GgmlType))[(int)ggmlType];
        string modelPath = $"ggml-{modelName.ToLower()}.bin";
        if (!File.Exists(modelPath))
        {
            Task.Run(async () => await DownloadModel(modelPath, ggmlType)).Wait();
        }

        Language = language;
        Threads = threads;

        var whisperFactory = WhisperFactory.FromPath(modelPath);
        Processor = whisperFactory.CreateBuilder()
            .WithLanguage(Language).WithThreads(Threads)
            .Build();
    }

    private static float[] ConstructParagraph(List<float[]> retainedData)
    {
        int totalLength = retainedData.SelectMany(s => s).Count();
        int prefixCut = retainedData
            .TakeWhile(data => data.Sum(Math.Abs) < (MINIMUM_NOISE / (SAMPLE_RATE / data.Length)))
            .SelectMany(s => s)
            .ToArray().Length;
        int suffixCut = END_AT - Math.Min(END_AT, KEEP_PUFFER);
        var paragraphData = retainedData
            .SelectMany(s => s)
            .Skip(prefixCut - Math.Min(prefixCut, KEEP_PUFFER))
            .Take(totalLength - prefixCut - suffixCut)
            .ToArray();

        return paragraphData;
    }

    private async Task Process(TaskQueue<float[], string> queue)
    {
        await queue.Next(async (input) =>
        {
            CurrentSamplesCount += input.Length;
            RetainedData.Add(input);

            // Check if there's actual "relevant" noise
            var noiseLevel = input.Sum(Math.Abs);
            if (noiseLevel < (MINIMUM_NOISE / (SAMPLE_RATE / input.Length)))
            {
                LastNoise += input.Length;
            }
            else
            {
                LastNoise = 0;
            }

            // Long enough no noise, check result.
            if (LastNoise >= END_AT)
            {
                LastNoise = 0;
                // Check if enough relevant samples are present
                if (CurrentSamplesCount < MINIMUM_RELEVANT + END_AT)
                {
                    CurrentSamplesCount = 0;
                    RetainedData.Clear();

                    return new Result<string>(EStatus.DROPPED, "");
                }

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                Console.WriteLine($"Preparing samples... ({CurrentSamplesCount})");
                CurrentSamplesCount = 0;

                // Construct the samples array (Also removes the segments without noise at the start & end)
                float[] paragraphData = ConstructParagraph(RetainedData);
                RetainedData.Clear();

                Console.WriteLine($"Processing relevant samples... ({paragraphData.Length})");
                // Process samples
                string text = "";
                await foreach (var segment in Processor.ProcessAsync(paragraphData))
                {
                    text += segment.Text;
                }
                stopwatch.Stop();
                Console.WriteLine($"Finishing processing ({paragraphData.Length / stopwatch.Elapsed.TotalMilliseconds}kS/s -> {stopwatch.Elapsed} seconds)");
                return new Result<string>(EStatus.DONE, text ?? "");
            }
            else
            {
                return new Result<string>(EStatus.WAIT_FOR_MORE, "");
            }
        });
    }

    internal async Task StartAsync()
    {
        var client = new HubClient(CLIENT_NAME);
        var queue = client.RegisterProcessor<float[], string>(EProcessor.STT);
        await client.StartAsync();

        queue.OnInputAsync(Process);

        await Task.Delay(-1);
    }
}
