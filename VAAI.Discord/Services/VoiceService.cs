using Discord;
using Discord.Audio;
using Microsoft.Extensions.Logging;

namespace VAAI.Discord.Services;

internal class VoiceService : IVoiceService
{
    private ILogger logger;
    private List<IAudioClient> clients = new List<IAudioClient>();

    public VoiceService(ILogger<VoiceService> logger)
    {
        this.logger = logger;
    }

    public void Connect(IVoiceChannel vc)
    {
        _ = Task.Run(async () =>
        {
            var tokenSource = new CancellationTokenSource();

            using var client = await vc.ConnectAsync();
            clients.Add(client);
            client.Disconnected += async (ex) =>
            {
                logger.LogInformation($"{client} disconnected");
                clients.Remove(client);
                await Task.Delay(0);
                tokenSource.Cancel();
            };
            try
            {
                await Task.Delay(-1, tokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                logger.LogInformation($"Closed {client}");
            }
        });
    }

    public async void Disconnect(IVoiceChannel vc)
    {
        await vc.DisconnectAsync();
    }
}
