using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Threading.Channels;
using VAAI.Library;
using VAAI.Shared.Enums;

namespace VAAI.Client
{
    internal class AudioClient
    {
        private readonly HubClient Client;
        private readonly int SampleRate;
        private readonly int Channels;
        private readonly ILogger Logger;
        private readonly Invoker Invoker;
        private readonly Listener Listener;

        public AudioClient(int sampleRate, int channels)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddConsole();
            });
            Logger = loggerFactory.CreateLogger<HubClient>();

            Client = new HubClient("NAudio");
            Invoker = Client.registerInvoker();
            Listener = Client.registerListener();
            Listener.OnSTT((message) =>
            {
                switch (message.Content.Status)
                {
                    case EStatus.DROPPED:
                        Logger.LogInformation($"Listener was informed that a message was dropped. ({message.Id})");
                        break;
                    case EStatus.WAIT_FOR_MORE:
                        Logger.LogInformation($"Listener was informed that STT has to wait for more content. ({message.Id})");
                        break;
                    case EStatus.DONE:
                        Logger.LogInformation($"Listener was informed that STT finished work ({message.Id}): {message.Content.Content}");
                        break;
                }
            });

            SampleRate = sampleRate;
            Channels = channels;
        }

        public async Task RecordMicrophoneAsync(Channel<byte[]> audioChannel, CancellationToken cancellationToken)
        {
            using var waveIn = new WaveInEvent();
            waveIn.WaveFormat = new WaveFormat(SampleRate, Channels);

            waveIn.DataAvailable += (sender, args) =>
            {
                var buffer = new byte[args.BytesRecorded];
                Array.Copy(args.Buffer, buffer, args.BytesRecorded);
                audioChannel.Writer.TryWrite(buffer);
            };

            waveIn.StartRecording();

            await Task.Delay(-1);

            waveIn.StopRecording();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await Client.StartAsync();

            _ = Task.Run(async () =>
            {
                var audioChannel = Channel.CreateUnbounded<byte[]>();
                _ = RecordMicrophoneAsync(audioChannel, cancellationToken);
                try
                {
                    var recordSeconds = 3;
                    var sampleSize = SampleRate * recordSeconds;
                    var samples = new float[sampleSize];
                    var currentIndex = 0;

                    await foreach (var buffer in audioChannel.Reader.ReadAllAsync(cancellationToken))
                    {
                        for (int i = 0; i < buffer.Length; i += 2)
                        {
                            var sampleValue = BitConverter.ToInt16(buffer, i) / Channels / (float)short.MaxValue;
                            samples[currentIndex] = sampleValue;

                            currentIndex++;
                            if (currentIndex >= sampleSize)
                            {
                                var id = await Invoker.InvokeSTT(samples);

                                currentIndex = 0;
                            }
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
                finally
                {
                    audioChannel.Writer.Complete();
                }
            });
        }
    }
}
