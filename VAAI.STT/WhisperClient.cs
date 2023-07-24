using VAAI.Library;
using Whisper.net;
using Whisper.net.Ggml;

namespace VAAI.STT
{
    internal class WhisperClient
    {
        private const string CLIENT_NAME = "Whisper";
        private const string MODELS_PATH = "";
        private readonly string Model;
        private readonly string Language;
        private readonly int Threads;

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
        }

        private async Task Process(TaskQueue<float[], string> queue)
        {
            var whisperFactory = WhisperFactory.FromPath(Model);
            var processor = whisperFactory.CreateBuilder()
                .WithLanguage(Language).WithThreads(Threads)
                .Build();
            while (true)
            {
                var now = DateTime.UtcNow;
                if (queue.HasTasks)
                {
                    var input = queue.InputQueue.Dequeue();
                    await foreach (var result in processor.ProcessAsync(input))
                    {
                        Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
                    }
                    queue.OutputQueue.Enqueue("");
                }
            }
        }

        public async Task StartAsync()
        {
            if (!File.Exists(Model))
            {
                await DownloadModel(Model, GgmlType.BaseEn);
            }

            var client = new HubClient(CLIENT_NAME);
            var queue = client.registerSTT();
            await client.StartAsync();

            await Process(queue);
        }
    }
}
