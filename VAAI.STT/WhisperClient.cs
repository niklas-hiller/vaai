using VAAI.Library;
using VAAI.Shared.Communication;
using VAAI.Shared.Enums;
using Whisper.net;
using Whisper.net.Ggml;

namespace VAAI.STT;

internal class WhisperClient
{
    private const string CLIENT_NAME = "Whisper";
    private const string MODELS_PATH = "";
    private readonly string Model;
    private readonly string Language;
    private readonly int Threads;
    private readonly string[] PAUSE = new string[] { "[ Silence ]", "[BLANK_AUDIO]" };
    private readonly WhisperProcessor Processor;
    private readonly List<float[]> RetainedData = new();

    private static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
        using var fileWriter = File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }

    public WhisperClient(GgmlType ggmlType, string language, int threads)
    {
        string modelName = Enum.GetNames(typeof(GgmlType))[(int)ggmlType];
        Model = $"{MODELS_PATH}ggml-{modelName.ToLower()}.bin";
        Language = language;
        Threads = threads;

        var whisperFactory = WhisperFactory.FromPath(Model);
        Processor = whisperFactory.CreateBuilder()
            .WithLanguage(Language).WithThreads(Threads)
            .Build();
    }

    public static float[] ConstructParagraph(List<float[]> retainedData)
    {
        int totalLength = 0;
        retainedData.ForEach(data =>
        {
            totalLength += data.Length;
        });
        float[] paragraphData = new float[totalLength];
        int index = 0;
        retainedData.ForEach(data =>
        {
            Array.Copy(data, 0, paragraphData, index, data.Length);
            index += data.Length;
        });

        return paragraphData;
    }

    private async Task Process(TaskQueue<float[], string> queue)
    {
        if (queue.HasTasks)
        {
            // Read Input
            var input = queue.InputQueue.Dequeue();
            List<SegmentData> segments = new();
            await foreach (var segment in Processor.ProcessAsync(input))
            {
                segments.Add(segment);
                Console.WriteLine($"{segment.Start}->{segment.End}: {segment.Text}");
            }

            // Evaluate Result
            Result<string> result;
            if (segments.All(segment => PAUSE.Any(prompt => segment.Text.Contains(prompt))))
            {
                if (RetainedData.Count > 0)
                {
                    float[] paragraphData = ConstructParagraph(RetainedData);
                    RetainedData.Clear();
                    string text = "";
                    await foreach (var segment in Processor.ProcessAsync(paragraphData))
                    {
                        text += segment.Text;
                    }
                    result = new Result<string>(EStatus.DONE, text);
                }
                else
                {
                    result = new Result<string>(EStatus.DROPPED, "");
                }
            }
            else
            {
                RetainedData.Add(input);
                result = new Result<string>(EStatus.WAIT_FOR_MORE, "");
            }

            queue.OutputQueue.Enqueue(result);
        }
    }

    public async Task StartAsync()
    {
        if (!File.Exists(Model))
        {
            await DownloadModel(Model, GgmlType.BaseEn);
        }

        var client = new HubClient(CLIENT_NAME);
        var queue = client.RegisterSTT();
        await client.StartAsync();

        queue.OnInputAsync(Process);

        await Task.Delay(-1);
    }
}
