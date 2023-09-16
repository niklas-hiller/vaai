using Microsoft.Extensions.Logging;
using VAAI.Library;

namespace VAAI.Discord;

internal class DiscordClient
{
    private readonly ILogger logger;
    private readonly AudioClient AudioClient;

    public DiscordClient()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });
        logger = loggerFactory.CreateLogger<DiscordClient>();

        this.AudioClient = new AudioClient(16000, 1);
    }

    public async Task StartAsync()
    {
        logger.LogInformation("Starting Application...");

        var tokenSource = new CancellationTokenSource();

        await AudioClient.StartAsync(tokenSource.Token);

        await Task.Delay(-1);
    }
}
