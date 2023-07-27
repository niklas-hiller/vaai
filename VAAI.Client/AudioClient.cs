using Microsoft.Extensions.Logging;
using NAudio.Wave;
using VAAI.Library;
using VAAI.Shared.Enums;

namespace VAAI.Client;

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
        Logger = loggerFactory.CreateLogger<AudioClient>();

        Client = new HubClient("NAudio");
        Invoker = Client.RegisterInvoker();
        Listener = Client.RegisterListener();
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

    public async Task RecordMicrophoneAsync()
    {
        using var waveIn = new WaveInEvent();
        waveIn.WaveFormat = new WaveFormat(SampleRate, Channels);

        waveIn.DataAvailable += async (sender, args) =>
        {
            // Copy buffer to prevent overwrite
            var buffer = new byte[args.BytesRecorded];
            Array.Copy(args.Buffer, buffer, args.BytesRecorded);

            // Convert buffer to samples
            var samples = new float[args.BytesRecorded];
            for (int i = 0; i < buffer.Length / 2; i++)
            {
                samples[i] = BitConverter.ToInt16(buffer, i * 2) / Channels / (float)short.MaxValue;
            }
            var id = await Invoker.InvokeSTT(samples);
        };

        waveIn.StartRecording();

        await Task.Delay(-1);

        waveIn.StopRecording();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await Client.StartAsync();

        _ = Task.Run(RecordMicrophoneAsync, cancellationToken);
    }
}
