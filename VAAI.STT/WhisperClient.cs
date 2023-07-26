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
        var startIndex = 0;
        int relevantLength = 0;

        retainedData.ForEach(data =>
        {
            // Remove unrelevant start.
            if (relevantLength == 0)
            {
                var noiseLevel = data.Sum(Math.Abs);
                if (noiseLevel < (MINIMUM_NOISE / (SAMPLE_RATE / data.Length)))
                {
                    startIndex++;
                    return;
                }
            }
            relevantLength += data.Length;
        });
        float[] paragraphData = new float[relevantLength - END_AT];

        int index = 0;
        for (int i = startIndex; index < relevantLength - END_AT; i++)
        {
            Array.Copy(retainedData[i], 0, paragraphData, index, retainedData[i].Length);
            index += retainedData[i].Length;
        }

        return paragraphData;
    }

    private async Task Process(TaskQueue<float[], string> queue)
    {
        if (queue.HasTasks)
        {
            // Read Input
            var input = queue.InputQueue.Dequeue();
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

                    queue.OutputQueue.Enqueue(new Result<string>(EStatus.DROPPED, ""));
                    return;
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
                Console.WriteLine($"Finishing processing ({stopwatch.Elapsed.Milliseconds / (paragraphData.Length / 1000)}kS/ms -> {stopwatch.Elapsed} seconds)");
                queue.OutputQueue.Enqueue(new Result<string>(EStatus.DONE, text));
            }
            else
            {
                queue.OutputQueue.Enqueue(new Result<string>(EStatus.WAIT_FOR_MORE, ""));
            }

        }
    }

    internal async Task StartAsync()
    {
        var client = new HubClient(CLIENT_NAME);
        var queue = client.RegisterSTT();
        await client.StartAsync();

        queue.OnInputAsync(Process);

        await Task.Delay(-1);
    }
}
